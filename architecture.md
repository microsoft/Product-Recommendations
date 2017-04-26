# Recommendations Service Architecture

<img src="images/architecture/appservice-diagram.png" align="inline" height="500px">

## Recommendations Web App

<img src="images/architecture/app-service-web.png" align="left" height="80px">

The recommendation web app is an [Azure App Service](https://azure.microsoft.com/en-us/services/app-service/web/) web application that exposes a 
RESTful API for creating (training) and managing models as well as getting recommendation using trained models (see [API Reference](api-reference.md)). 
When a new model creation (training) request is received, the web app creates an entry for the new model in the [Model Registry](##ModelRegistry) Azure 
Storage Table, with a status of *Created*, and enqueues a new message the *[Train Model](##TrainModelQueue)* Azure Storage Queue, to be processed by 
the [Recommendations Web Job](##RecommendationsWebJob). The new model id (GUID) is returned in the HTTP response.

The web app also exposes APIS for listing, retrieving models by querying\updating the [Model Registry](##ModelRegistry).
When a delete model request is received, the model record is removed from the [Model Registry](##ModelRegistry) and a new message is enqueued 
to the *[Delete Model](##DeleteModelQueue)* Azure Storage Queue, to be processed by the [Recommendations Web Job](##RecommendationsWebJob).

When handling a get recommendations request, the [Model Provider](##ModelProvider) loading the trained model into memory (if not already cached) 
and use it to produce recommendations. For more information about the recommendation algorithm, see [SAR](sar.md).

## Recommendations Web Job

<img src="images/architecture/app-service-webjob.jpg" align="left" height="100px">

The recommendations web job is an [Azure Web Job](https://docs.microsoft.com/en-us/azure/app-service-web/websites-webjobs-resources) process is 
defined on every instance of the Web App.

The web job listens to the *[Train Model](##TrainModelQueue)* for new model training requests.
Once a new message is found, the web job queries for the [Model Registry](##ModelRegistry) for the model details and training parameters. 
The process of training a model then starts (see [Model Training Flow](##ModelTrainingFlow)) and continuously updates the model status in 
[Model Registry](##ModelRegistry), making the progress accessible via the API. Once completed, the model's status is updated to either *Completed* 
or *Failed*. If successful, the trained model is serialized and uploaded to Azure Blob Storage, using the [Model Provider](##ModelProvider).
Models with status *Completed* can be used for getting recommendations using the API.

The web job also listens to the *[Delete Model](##DeleteModelQueue)* for model deletion requests. 
Once a new message is found, the [Model Provider](##ModelProvider) is used to deleted the trained model blob (if exists) from Azure Storage.

## Model Registry
## Model Provider
## Train Model Queue
## Delete Model Queue
## Model Training Flow
 

