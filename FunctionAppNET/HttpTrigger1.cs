using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;  
using System.Collections.Generic;  
using System.IO;  
using System.Linq;  
using System.Text.Json;  
using System.Threading.Tasks;  
using Newtonsoft.Json;
using My.Functions.OutputNamespace;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Schema.Generation;
using OpenAI.Chat;
using System.ClientModel;
using Azure.Core;


//https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-vs-code-csharp

namespace My.Functions
{
     // Define the Output class here  
    
    namespace OutputNamespace  
    {  
            public class OutputList  
            {  
                public required List<Output> Values { get; set; }  
            }
            public class Output  
            {  
                public required string RecordId { get; set; }  
                public required Data Data { get; set; }  
            }  
    
            public class Data  
            {  
                public required List<string> Loads { get; set; }  
            }  
    }  
     namespace RequestNamespace  
    {  
        public class Root  
        {  
            public required List<Value> Values { get; set; }  
        }  
  
        public class Value  
        {  
            public required string RecordId { get; set; }  
            public required Data Data { get; set; }  
        }  
  
        public class Data  
        {  
            public required List<Text> Text { get; set; }  
        }  
  
        public class Text  
        {  
            public required string Content { get; set; }  
            public required Sections Sections { get; set; }  
            public int OrdinalPosition { get; set; }  
        }  
  
        public class Sections  
        {  
            public required string H1 { get; set; }  
            public required string H2 { get; set; }  
            public required string H3 { get; set; }  
            public required string H4 { get; set; }  
            public required string H5 { get; set; }  
            public required string H6 { get; set; }  
        }  
    }  
    
    public class OpenAIResponseLoads()  
    {  
        public required List<string> Loads { get; set; }  
    }
    
    public class HttpTrigger1
    {
        private readonly ILogger<HttpTrigger1> _logger;

        public HttpTrigger1(ILogger<HttpTrigger1> logger)
        {
            _logger = logger;
        }

        [Function("HttpTrigger1")]  
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)  
        {  
            _logger.LogInformation("C# HTTP trigger function processed a request.");  
  
            try  
            {  
                // Parse the incoming request body asynchronously  
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();  
                //var body = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);  
                 var body = JsonConvert.DeserializeObject<RequestNamespace.Root>(requestBody);

                _logger.LogInformation("Request body parsed successfully");
                _logger.LogInformation($"Request body: {requestBody}");
  
                if (body != null)  
                {  
                    string recordId = body.Values.First().RecordId;
                    

                    List<String>  result = await GetLoads(body);

                    //List<String> result = ["L123456", "L123456", "L123456"];  

                     var output = new OutputList  
                    {  
                        Values = new List<Output>  
                        {  
                            new Output  
                            {  
                                RecordId = recordId,  
                                Data = new Data  
                                {  
                                    Loads = result  
                                }  
                            }  
                        }  
                    };  

                    _logger.LogInformation($"Output: {JsonConvert.SerializeObject(output)}");
                    _logger.LogInformation(output.ToString());

                    return new OkObjectResult(output);  
                }  
                else  
                {  
                    return new BadRequestObjectResult("The body of the request could not be parsed");  
                }  
            }  
            catch (Newtonsoft.Json.JsonException)  
            {  
                return new BadRequestObjectResult("The body of the request could not be parsed");  
            }  
            catch (KeyNotFoundException)  
            {  
                return new BadRequestObjectResult("Skill configuration error. Endpoint, key, and model_id required.");  
            }  
            catch (Exception ex)  
            {  
                _logger.LogError(ex, "An error occurred while processing the request.");  
                return new BadRequestObjectResult("An unexpected error occurred.");  
            }  
        }  

         static string GetEnvironmentVariable(string key)
        {
                return Environment.GetEnvironmentVariable(key) ?? throw new InvalidOperationException($"Environment variable '{key}' not found.");
        }
    public async Task<List<string>> GetLoads(RequestNamespace.Root body)  
    {  
        try  
        {  
            // Step 1: Validate the input body  
            if (body?.Values == null || !body.Values.Any())  
            {  
                _logger.LogError("Input body is null or 'Values' array is empty.");  
                throw new ArgumentException("Invalid input body: 'Values' array is required.");  
            }  
    
            var data = body.Values.First().Data?.Text?.FirstOrDefault();  
            var content = data?.Content;  
    
            if (string.IsNullOrEmpty(content))  
            {  
                _logger.LogError("Content is null or empty.");  
                throw new ArgumentException("Content is required and cannot be null or empty.");  
            }  
    
            _logger.LogInformation($"Content: {content}");  
    
            // Step 2: Retrieve environment variables  
            string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");  
            if (string.IsNullOrEmpty(endpoint))  
            {  
                _logger.LogError("AZURE_OPENAI_ENDPOINT environment variable is missing or empty.");  
                throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT environment variable is required.");  
            }  
    
            _logger.LogInformation($"Endpoint: {endpoint}");  
    
            string key = GetEnvironmentVariable("AZURE_OPENAI_API_KEY");  
            if (string.IsNullOrEmpty(key))  
            {  
                _logger.LogError("AZURE_OPENAI_API_KEY environment variable is missing or empty.");  
                throw new InvalidOperationException("AZURE_OPENAI_API_KEY environment variable is required.");  
            }  
    
            _logger.LogInformation("Retrieved API key.");  
    
            // Step 3: Create the OpenAI client  
            var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));  
            _logger.LogInformation("Created OpenAI client.");  
    
            var client = openAIClient.GetChatClient("gpt-4o");  
            _logger.LogInformation("Created Chat client.");  
    
            // Step 4: Prepare chat messages  
            var chat = new List<ChatMessage>  
            {  
                new SystemChatMessage("Objective: Extract unique load identifier from bills of lading and compile them into a distinct, structured list. A 'load' refers to the specific cargo or shipment. The output should exclude duplicates and irrelevant information. example of loads would be: L12345"),  
                new UserChatMessage(content)  
            };  
    
            // Step 5: Generate JSON schema for structured response  
            JSchemaGenerator generator = new JSchemaGenerator();  
            var jsonSchema = generator.Generate(typeof(OpenAIResponseLoads)).ToString();  
    
            _logger.LogInformation("Generated JSON schema.");  
    
            // Step 6: Call OpenAI API  
            var chatUpdates = await client.CompleteChatAsync(  
                chat,  
                new ChatCompletionOptions  
                {  
                    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(  
                        "openAIResponseLoads",  
                        BinaryData.FromString(jsonSchema))  
                });  
    
            _logger.LogInformation("Received response from OpenAI API.");  
    
            // Step 7: Deserialize the response  
            var responseContent = chatUpdates.Value.Content[0].Text;  
            _logger.LogInformation($"Response content: {responseContent}");  
    
            var responseObject = JsonConvert.DeserializeObject<OpenAIResponseLoads>(responseContent);  
            _logger.LogInformation($"Deserialized response object: {responseObject}");  
    
            if (responseObject?.Loads != null && responseObject.Loads.Any())  
            {  
                _logger.LogInformation($"Loads: {string.Join(", ", responseObject.Loads)}");  
                return responseObject.Loads;  
            }  
            else  
            {  
                _logger.LogWarning("Response object loads are null or empty.");  
                return new List<string>();  
            }  
        }  
        catch (Exception ex)  
        {  
            // Log the exception and return a default empty list  
            _logger.LogError(ex, $"An error occurred while processing the GetLoads request: {ex.Message}");  
            return new List<string>();  
        }  
    }  
        

 
    }
}
