# Load Finding AI Solution

This project provides an AI-powered solution for automatically extracting and identifying load references (e.g., "L12345", "L67890") from documents using Azure AI Search and Azure OpenAI. The solution uses custom skills integrated with Azure AI Search to intelligently process documents and extract load numbers for logistics and shipping operations.

## Overview

The solution consists of several key components that work together:

- **Azure AI Search**: Indexes documents and orchestrates the processing pipeline
- **Custom Function Apps**: 
  - Python Function App (`FunctionApp/`) - Custom skill for load extraction using OpenAI
  - .NET Function App (`FunctionAppNET/`) - Alternative .NET implementation 
- **Jupyter Notebooks**: Interactive setup and testing environments
  - `001_PullIndexCreationNET.ipynb` - .NET notebook for AI Search configuration
  - `001_PullIndexCreationPython.ipynb` - Python notebook for AI Search setup
- **Infrastructure as Code**: Bicep templates for Azure resource deployment

### Architecture Flow

1. Documents are uploaded to Azure Blob Storage
2. Azure AI Search indexer processes documents using a skillset that includes:
   - Document Intelligence Layout skill for text extraction
   - Custom WebAPI skill (Function Apps) for load identification
   - Text splitting and embedding capabilities
3. Custom Function Apps use Azure OpenAI to analyze text and extract load references
4. Processed results are stored in the search index for querying

## Prerequisites

### Required Azure Services
- **Supported Regions**: East US, West US2, West Europe, North Central US
- Azure Subscription with sufficient quotas
- Resource Group in one of the supported regions

### Development Environment

#### For .NET Components
- .NET 8 SDK
- Visual Studio Code with Polyglot extension (for .NET Interactive notebooks)
- Azure Functions Core Tools v4

#### For Python Components  
- Python 3.8 or higher
- pip (Python package manager)

#### General Requirements
- Azure CLI
- Bicep CLI
- Git

## Setup Instructions

### 1. Clone the Repository
```bash
git clone <repository-url>
cd loadfinding
```

### 2. Deploy Azure Resources

Navigate to the ResourceDeployment directory and follow the [Resource Deployment Guide](ResourceDeployment/README.md):

```bash
cd ResourceDeployment

# Deploy core resources
az deployment group create \
  --resource-group <your-resource-group> \
  --template-file main.bicep \
  --parameters openAiServiceName=<openai-name> storageAccountName=<storage-name> searchServiceName=<search-name>

# Configure permissions  
az deployment group create \
  --resource-group <your-resource-group> \
  --template-file permissions.bicep \
  --parameters userPrincipalName=<your-user-id> openAiServiceName=<openai-name> storageAccountName=<storage-name> searchServiceName=<search-name>
```

### 3. Deploy Azure OpenAI Models

Deploy the required models in Azure OpenAI Studio:
- **gpt-4o** (for load extraction)
- **text-embedding-ada-002** (for document embeddings)

### 4. Install Dependencies

#### For Python Components
```bash
# Root level dependencies for notebooks
pip install -r requirements.txt

# Function App dependencies
cd FunctionApp
pip install -r requirements.txt
```

#### For .NET Components
```bash
cd FunctionAppNET
dotnet restore
```

### 5. Configure Environment Variables

Create a `.env` file in the root directory with the following variables:

```env
# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://<your-openai-service>.openai.azure.com/
AZURE_OPENAI_API_KEY=<your-openai-key>
AZURE_OPENAI_EMBEDDING_MODEL_NAME=text-embedding-ada-002
AZURE_OPENAI_EMBEDDING_DIMENSIONS=1536
AZURE_OPENAI_SYSTEM_PROMPT=Extract all load numbers from the text. Load numbers typically follow patterns like L12345, L67890, etc.

# Azure AI Search Configuration  
AZURE_SEARCH_SERVICE_NAME=<your-search-service>
AZURE_SEARCH_INDEX=<your-index-name>
AZURE_SEARCH_API_KEY=<your-search-key>

# Azure Storage Configuration
BLOB_CONNECTION_STRING=<your-storage-connection-string>
BLOB_CONTAINER_NAME=<your-container-name>

# Azure AI Services
AZURE_AI_SERVICES_ENDPOINT=https://<your-region>.api.cognitive.microsoft.com/
```

### 6. Deploy Function Apps

#### Python Function App
```bash
cd FunctionApp
func azure functionapp publish <your-python-function-app-name>
```

#### .NET Function App
```bash
cd FunctionAppNET  
func azure functionapp publish <your-dotnet-function-app-name>
```

## Usage

### Setting Up AI Search Index

1. Open the appropriate Jupyter notebook:
   - For .NET: `001_PullIndexCreationNET.ipynb`
   - For Python: `001_PullIndexCreationPython.ipynb`

2. Follow the step-by-step cells to:
   - Create data sources
   - Define search indexes  
   - Configure skillsets with custom skills
   - Set up and run indexers

### Processing Documents

1. Upload documents to your configured blob storage container
2. The indexer will automatically process new documents
3. Load references will be extracted and stored in the search index
4. Query the index to retrieve documents and their associated load numbers

### Testing Custom Skills

The Function Apps can be tested independently by sending HTTP POST requests with the expected custom skill input format.

## Dependencies

### Python Requirements
```
python-dotenv
azure-search-documents==11.6.0b8  
azure-storage-blob
azure-identity
openai
azure-ai-projects
azure-functions
langchain
pandas
pydantic
tiktoken
```

### .NET Requirements
- .NET 8.0
- Microsoft.Azure.Functions.Worker 2.0.0
- Azure.AI.OpenAI 2.1.0
- Azure.Identity 1.12.0
- Newtonsoft.Json 13.0.1

## Additional Resources

- [Azure AI Search Custom Skills Documentation](https://docs.microsoft.com/en-us/azure/search/cognitive-search-custom-skill-interface)
- [Azure OpenAI Service Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)
- [Resource Deployment Details](ResourceDeployment/README.md)