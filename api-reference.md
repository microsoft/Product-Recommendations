# API Reference
Once you deploy the service, you will be able to navigate to the API Reference (Swagger definition) for your 
newly created service by going to http://\<root url\>/swagger/ . You will get the root url upon deploying your service.

## Authentication
When calling the service, you will need to set authentication headers to pass the API key you are provided 
as part of the set-up process.

If you did not copy the keys at the time of setting up the preconfigured solution, you can find those by:
1. Logging into the [Azure Portal](portal.azure.com/)
1. Navigating to the resource group that was created by the solution. The resource group is named after the deployment name you provided when you set up the solution.
1. There navigate to the App Service.
1. Click on **Application Settings**
1. The keys are under the **App settings** section, they are called *modelKey* and *recommenderKey*.

The *modelKey* is the key that can be used for all API operations, the *recommenderKey* can only be used to 
get recommendations, so this is the key you would use on the client or website requesting recommendations.

Sample authentication header:

```
Authentication: Basic <yourKeyGoesHere>
```


## Create a model
*POST /api/models*

Starts the training process that creates a new model that you can later on query for
recommendations. Before triggering a build you first must upload catalog and usage data
to a blob storage location.

Triggering a new build is an asynchronous operations. Once a build is triggered you
will receive a *Location* header that you can reference to get information about the model,
including the status of the training process.

See the "Get model information" API below.

The algorithm used to create this model is called SAR (Smart Adaptive Recommendations). 

The body of the message should contain the following parameters:

|  Parameter  | Description | Valid values (default)    
|-------------|-------------|------------------------------
| description | Textual description | String 
| baseContainerSasUri| SAS Uri to the container that will contain the catalog and transaction data | String 
| trainCatalogFileRelativeLocation |  Relative path to the catalog file | String
| trainUsageFolderRelativeLocation |  Relative path to the virtual directory that contains the usage file(s) | String
| supportThreshold | How conservative the model is. Number of co-occurrences of items to be considered for modeling. | 3-50 (6)
| cooccurrenceUnit | Indicates how to group usage events before counting co-occurrences. A 'User' cooccurrence unit will consider all items purchased by the same user as occurring together in the same session. A 'Timestamp' cooccurrence unit will consider all items purchased by the same user in the same time as occurring together in the same session. | *User*, *Timestamp* (Default: User)
| similarityFunction| Defines the similarity function to be used by the build. Lift favors serendipity, Co-occurrence favors predictability, and Jaccard is a nice compromise between the two. | *Cooccurrence*, *Lift*, *Jaccard* (Lift)
| enableColdItemPlacement |  Indicates if the recommendation should also push cold items via feature similarity.  | True, False (False)
|enableColdToColdRecommendations | Indicates whether the similarity between pairs of cold items (catalog items without usage) should be computed. If set to false, only similarity between cold and warm item will be computed, using catalog item features. Note that this configuration is only relevant when enableColdItemPlacement is set to true. | True, False (False)
| enableUserAffinity | For personalized recommendations, it defines whether the event type and the time of the event should be considered as input into the scoring. | True, False (False)
| enableBackfilling | Backfill with popular items when the system does not find sufficient recommendations. | True, False (True)
| allowSeedItemsInRecommendations |  Allow seed items (items in the input or in the user history) to be returned as recommendation results. | True, False (False)
| decayPeriodInDays | The decay period in days. The strength of the signal for events that are that many days old will be half that of the most recent events. | 30


Sample body:
```
{
    "description": "Simple recommendations model",
    "baseContainerSasUri": "https://myreco.blob.core.windows.net/input-files?st=2017-03-14T11%3A07%3A00Z&se=2018-03-16T11%3A07%3A00Z&sp=rwdl&sv=2015-12-11&sr=c&sig=ztvAyzQqCPLQ7gDGGz9x0pNnfu8TKD9HSG5EDp9C0w%3D",
    "trainCatalogFileRelativeLocation": "books.csv",
    "trainUsageFolderRelativeLocation": "booksTrainUsage",
    "evaluationUsageFolderRelativeLocation": "booksTestUsage",
    "supportThreshold": 6,
    "cooccurrenceUnit": "User",
    "similarityFunction": "Jaccard",
    "enableColdItemPlacement": false,
    "enableColdToColdRecommendations": false,
    "enableUserAffinity": true,
    "allowSeedItemsInRecommendations": true,
    "enableBackfilling": true,
    "decayPeriodInDays": 30
}
```

## Get model information
*GET /api/models*

Returns metadata about the model, including the training status,  parameters used to 
build the model, the time of creation, statistics about the model and evaluation results.


## List models
*GET /api/models*

Will return basic properties for each of the existing models.

## Delete a model
*DELETE /api/models/{modelId}*

Deletes the model specified.

You cannot delete the default build. If you try to delete the default build you will get an error.
The model should be updated to a different default build before you delete it. 

## Get item-to-item recommendations from a model
*GET /api/models/{modelId}/recommendations*

Gets item-to-item recommendations for the model specified. 

Empty recommendations may be returned if none of the items are in the catalog or if the trained model
did not have sufficient data to provide recommendations for the items. 

*Request parameters*

|  Request Parameter  | Description    | Valid values 
|-------------|-------------------|-----------------
|  itemId     |  Seed item Id | String
| numberOfResults | Number of recommended items to return | Integer, 2 to 100

## Get personalized recommendations from a model 
*POST /api/models/{modelId}/recommendations*

Gets personalized recommendations for the model specified, given a set of recent events for a particular user.
The *recent events* -- or recent history -- should be passed in the body of the request in the following format:

```
{
  events: [ 
    {  
       itemId: "string", 
       eventType: "enum",
       customEventTypeWeight: "float", 

       timestamp: "datetime”
   } ]
}
```

## Set default model
*PUT /api/models/default* 

## Get default model
*GET /api/models/default* 

## Get item-to-item recommendations from the default model
*GET /api/models/default/recommendations*

Once a default build is set, similar to getting item-to-item recommendations, but using the default model.

## Get personalized recommendations from default model
*POST /api/models/default/recommendations*

Once a default build is set, similar to getting personalized recommendations, but using the default model.
