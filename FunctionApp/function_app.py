import azure.functions as func
import logging
import json
from langchain.text_splitter import TokenTextSplitter
import pandas as pd
import os
from pydantic import BaseModel
from openai import AzureOpenAI
import os

class Results(BaseModel):
            Loads: list[str]

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)

@app.route(route="http_trigger")
def http_trigger(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    try:
        logging.info('version 6')
        body = req.get_json()
        if body:
            logging.info('about to compose response')
            result = compose_response(body)
            return func.HttpResponse(result, mimetype="application/json")
        else:
            return func.HttpResponse(
                "The body of the request could not be parsed",
                status_code=400
            )
    except ValueError:
        return func.HttpResponse(
             "The body of the request could not be parsed",
             status_code=400
        )
    except KeyError:
        return func.HttpResponse(   
             "Skill configuration error. Endpoint, key and model_id required.",
             status_code=400
        )
    except AssertionError  as error:
        return func.HttpResponse(   
             "Request format is not a valid custom skill input",
             status_code=400
        )

def checkforLoads(text):
    logging.info('checkforLoads')
    # Placeholder for actual embedding logic
    # In a real scenario, you would use a model to get the embeddings
    # For now, we just return the text as is
    # Example: loads = model.embed(text)
    # Here we just return a dummy list for demonstration purposes
    #loads = ['load1', 'load2']  # Placeholder for actual embedding
    client = AzureOpenAI(azure_endpoint = os.getenv("AZURE_OPENAI_ENDPOINT"), api_key=os.getenv("AZURE_OPENAI_API_KEY"),  api_version="2024-10-21")


    system_prompt = os.getenv("AZURE_OPENAI_SYSTEM_PROMPT")
    logging.info('system_prompt')
    logging.info(system_prompt)
    
    completion = client.beta.chat.completions.parse(
            model="gpt-4o", # replace with the model deployment name of your gpt-4o 2024-08-06 deployment
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": text},
            ],
            response_format=Results,
        )

    logging.info('completion')
    logging.info(completion)

    event = completion.choices[0].message.parsed

    logging.info('event')
    logging.info(event)

    return event.Loads

def text_split(relevant_data):
    logging.info('text_split_embedd')
    text_splitter = TokenTextSplitter(chunk_size=2000, chunk_overlap=0)
    texts = text_splitter.split_text(relevant_data)
    
    unique_loads = set()  # Use a set to store unique loads  
    for text in texts:  
        myloads = checkforLoads(text)  
        unique_loads.update(myloads)  # Add loads to the set to ensure uniqueness  
      
    return list(unique_loads)  # Convert the set back to a list if needed  


def compose_response(body):
    logging.info('in compose response')
    logging.info('loaded body')
    values = body['values']
    logging.info('got values')

    results = {}
    results["values"] = []

    logging.info('go through values')
    for value in values:
        output_record = transform_value(value)
        if output_record != None:
            results["values"].append(output_record)
            break
    return json.dumps(results, ensure_ascii=False)


def transform_value(value):
    logging.info('in transform_value')
    
    try:
        recordId = value['recordId']
    except AssertionError  as error:
        return None

    # Validate the inputs
    try:  
        logging.info(value)       
        assert ('data' in value), "'data' field is required."
        data = value['data']   
    except AssertionError  as error:
        return (
            {
            "recordId": recordId,
            "data":{},
            "errors": [ { "message": "Error:" + error.args[0] }   ]
            })
    try:             
        source = value['data']['text']


        unique_loads = set()
        for item in source:  
            logging.info('*******item')
            if 'content' in item:  # Check if 'content' key exists  
                content = item['content']  
                logging.info("Content:")  
                logging.info(content)  
                #pass to openai and get back a list of loads.
                output_loads = text_split(content)
                print("output_loads")
                print(output_loads)
        
                for load in output_loads:  
                    if load not in unique_loads:  # Optional logging for newly added loads  
                        logging.info(f"Adding new load: {load}")  
                    unique_loads.add(load)

        unique_loads_list = list(unique_loads)
        
    except Exception as e:
        logging.info(e)
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "Could not complete operation for record."  } , {e}  ]
            })

    output = {
            "recordId": recordId,
            "data": {
                "loads": unique_loads_list
                    }
            }
    logging.info('output')
    logging.info(output)
    return (output)