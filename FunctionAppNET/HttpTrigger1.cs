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
                    
                    var data = GetLoads(body);

                    List<String> result = ["L123456", "L123456", "L123456"];  

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
        public async Task<string> GetLoads(RequestNamespace.Root body)  
        {  
            var data =  body.Values.First().Data.Text.FirstOrDefault();
            var content = data?.Content;
            _logger.LogInformation($"Content: {content}");

            // Create the clients
            string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

           
            string key = GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

            var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
            var client = openAIClient.GetChatClient("gpt-4o");

            // Create a chat with initial prompts
            var chat = new List<ChatMessage>
            {
                new SystemChatMessage("Extract the event information and projected weather."),
                new UserChatMessage("Alice and Bob are going to a science fair in Seattle on June 1st, 2025.")
            };

            // Get the schema of the class for the structured response
            JSchemaGenerator generator = new JSchemaGenerator();
            var jsonSchema = generator.Generate(typeof(OpenAIResponseLoads)).ToString();

            // Get a completion with structured output
            var chatUpdates = await client.CompleteChatAsync(
                chat,
                new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                        "openAIResponseLoads",
                        BinaryData.FromString(jsonSchema))
                });

            // Deserialize the response to the defined class
            var responseContent = chatUpdates.GetRawResponse().Content.ToString();
            _logger.LogInformation($"Response content: {responseContent}");
            var responseObject = JsonConvert.DeserializeObject<OpenAIResponseLoads>(responseContent);
            _logger.LogInformation($"Response object: {responseObject}");
            if (responseObject?.Loads != null)
            {
                _logger.LogInformation($"Response object loads: {string.Join(", ", responseObject.Loads)}");
            }
            else
            {
                _logger.LogInformation("Response object loads is null or empty.");
            }


            // Return a default or meaningful string value
            return "Loads processed successfully.";
        }
        

       

 
    }
}
