# API Reference

## Swagger UI
Once you deploy the service, you will be able to navigate to the API Reference (Swagger definition)
of your newly created service by going to http://\<root url\>/swagger/ . You will get the root url
upon deploying your service.

The swagger UI is the authoritative definition of the API interface -- but we have duplicated a
description of the API reference here so you can understand what the service will look like before
you actually deploy it.


## Authentication
When calling the service, you will need to set authentication headers with the API key that is provided as part of the set-up process.

If you did not copy the keys at the time of setting up the preconfigured solution, you can find those by:
1. Logging into the [Azure Portal](https://portal.azure.com/)
2. Navigating to the resource group that was created by the solution. The resource group is named after the deployment name you provided when you set up the solution.
3. Then navigate to **App Service**.
4. Click on **Application Settings**
5. The keys are under the **App settings** section, they are called *adminPrimaryKey*, *adminSecondaryKey*, *recommendPrimaryKey* and *recommendSecondaryKey*.

The *admin\*Key* are keys that can be used for all API operations, the *recommend\*Key* keys can only be used to 
get recommendations, so these are the keys you would use in client applications or websites requesting recommendations.

Sample authentication header:

```
x-api-key: <yourKeyGoesHere>
```


## Create a model
*POST /api/models*

Starts the training process that creates a new model that you can later on query for
recommendations. Before creating a model you first must upload usage data, and optionally the catalog and evaluation files,
to the new blob storage account created by the preconfigured solution.

Creating a new model is an asynchronous operation. Once a model has been created you
will receive a *Location* header that you can reference to get information about the model,
including the status of the training process.

See the "Get model information" API below.

The algorithm used to create this model is called SAR (Smart Adaptive Recommendations). 

The body of the message should contain the following parameters:

|  Parameter  | Description | Valid values (default)    
|-------------|-------------|------------------------------
| description | Textual description | String (Current time)
| blobContainerName| Name of the container where the catalog, usage data and evaluation data are stored. Note that this container must be in the storage account created by the preconfigured solution | String 
| catalogFileRelativeLocation |  Relative path to the catalog file | String (null)
| usageFolderRelativeLocation |  Relative path to the virtual directory that contains the usage file(s) to be used for training | String
| evaluationUsageFolderRelativeLocation |  Relative path to the virtual directory that contains the usage file(s) to be used for evaluation | String (null)
| supportThreshold | How conservative the model is. Number of co-occurrences of items to be considered for modeling. | 3-50 (6)
| cooccurrenceUnit | Indicates how to group usage events before counting co-occurrences. A 'User' cooccurrence unit will consider all items purchased by the same user as occurring together in the same session. A 'Timestamp' cooccurrence unit will consider all items purchased by the same user in the same time as occurring together in the same session. | *User*, *Timestamp* (User)
| similarityFunction| Defines the similarity function to be used by the model. Lift favors serendipity, Co-occurrence favors predictability, and Jaccard is a nice compromise between the two. | *Cooccurrence*, *Lift*, *Jaccard* (Lift)
| enableColdItemPlacement |  Indicates if recommendations should also push cold items via feature similarity.  | True, False (False)
| enableColdToColdRecommendations | Indicates whether the similarity between pairs of cold items (catalog items without usage) should be computed. If set to false, only similarity between cold and warm items will be computed, using catalog item features. Note that this configuration is only relevant when enableColdItemPlacement is set to true. | True, False (False)
| enableUserAffinity | For personalized recommendations, it defines whether the event type and the time of the event should be considered as input into the scoring. | True, False (False)
| enableBackfilling | Backfill with popular items when the system does not find sufficient recommendations. | True, False (True)
| allowSeedItemsInRecommendations |  Allow seed items (items in the input or in the user history) to be returned as recommendation results. | True, False (False)
| decayPeriodInDays | The decay period in days. The strength of the signal for events that are that many days old will be half that of the most recent events. | Integer (30)



Sample body:
```
{
    "description": "Simple recommendations model",
    "blobContainerName": "input-files",
    "catalogFileRelativeLocation": "books.csv",
    "usageFolderRelativeLocation": "booksTrainUsage",
    "evaluationUsageFolderRelativeLocation": "booksTestUsage",
    "supportThreshold": 6,
    "cooccurrenceUnit": "User",
    "similarityFunction": "Jaccard",
    "enableColdItemPlacement": true,
    "enableColdToColdRecommendations": false,
    "enableUserAffinity": true,
    "allowSeedItemsInRecommendations": true,
    "enableBackfilling": true,
    "decayPeriodInDays": 30
}
```

## Get model information
*GET /api/models/{modelId}*

Returns metadata about the model, including the training status, parameters used to
build the model, the time of creation, statistics about the model and evaluation results.

## List models
*GET /api/models*

Return model metadata for each existing model.

## Delete a model
*DELETE /api/models/{modelId}*

Deletes the specified model.

If you delete the default model then there will be no default model until it is set again. Any recommendation requests to the default model will fail.

## Get item-to-item recommendations from a model for a single item
*GET /api/models/{modelId}/recommend*

Gets item-to-item recommendations for the model specified.

Empty recommendations may be returned if none of the items are in the catalog or if the model
did not have sufficient training data to provide recommendations for the item.

*Request parameters*

|  Request Parameter  | Description    | Valid values (default)
|-------------|-------------------|-----------------
|  itemId     |  Seed item Id | String
| recommendationCount | Number of recommended items to return | 1-100 (10)

## Get personalized recommendations from a model 
*POST /api/models/{modelId}/recommend*

Gets personalized recommendations for the model specified, given a set of recent events for a particular user.
The *recent events* -- or recent history -- should be passed in the body of the request in the following format:

| Event Member | Description   | Valid values (default)
|--------------|---------------|-----------------
|    itemId    |  Seed item Id | String
|   eventType  |  Event type | Click, RecommendationClick, AddShopCart, RemoveShopCart, Purchase (Click)
|   timestamp  |  Timestamp of event | Timestamp (Now)
|    weight    |  Custom relative signal strength of event | float (null)

```
[
    {
       "itemId": "ItemId123",
       "eventType": "Click",
       "timestamp": "2017-01-31 23:59:59"
    },
    {
       "itemId": "ItemId456",
       "eventType": "Purchase",
    },
    {
       "itemId": "ItemId789",
       "weight": 2.3,
    },
    {
       "itemId": "ItemId135",
    }
]
```

## Set default model
*POST /api/models/default* 

Sets the specified model as the default model.

## Get default model
*GET /api/models/default* 

Gets the metadata of the default model.

## Get item-to-item recommendations from the default model
*GET /api/models/default/recommend*

Once a default model is set, similar to getting item-to-item recommendations, but using the default model.

## Get personalized recommendations from default model
*POST /api/models/default/recommend*

Once a default model is set, similar to getting personalized recommendations, but using the default model.
