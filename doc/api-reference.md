# API Reference

## Swagger UI
Once you deploy the service, you will be able to navigate to the API Reference (Swagger definition)
of your newly created service by going to **https://<root_url>/swagger** . You will get the root url
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


## Create (train) a new model
*POST /api/models*

Starts the training process that creates a new model that you can later on query for
recommendations. Before creating a model you first must upload usage data, and optionally the catalog and evaluation files,
to the new blob storage account created by the preconfigured solution.

Creating a new model is an asynchronous operation. Once a model has been created you
will receive a *Location* header that you can reference to get information about the model,
including the status of the training process.

See the "Get model information" API below.

The algorithm used to create this model is called SAR (Smart Adaptive Recommendations). Optionally model metrics are computed if *evaluationUsageFolderRelativeLocation* is provided. See [Model Evaluation](model-evaluation.md) for more details.

The body of the message should contain the following parameters:

|  Parameter  | Description | Valid values (default)    
|-------------|-------------|------------------------------
| description | Textual description | String (Current time)
| blobContainerName| Name of the container where the catalog, usage data and evaluation data are stored. Note that this container must be in the storage account created by the preconfigured solution | String 
| catalogFileRelativeLocation |  Relative path to the catalog file. See [Catalog file format](#catalog-file-schema) | String (null)
| usageFolderRelativeLocation |  Relative path to the virtual directory that contains the usage file(s) to be used for training. See [Usage events file format](#usage-events-file-schema) | String
| evaluationUsageFolderRelativeLocation |  Relative path to the virtual directory that contains the usage file(s) to be used for evaluation. See [Usage events file format](#usage-events-file-schema) | String (null)
| supportThreshold | How conservative the model is. Number of co-occurrences of items to be considered for modeling. | 3-50 (6)
| cooccurrenceUnit | Indicates how to group usage events before counting co-occurrences. A 'User' cooccurrence unit will consider all items purchased by the same user as occurring together in the same session. A 'Timestamp' cooccurrence unit will consider all items purchased by the same user in the same time as occurring together in the same session. | *User*, *Timestamp* (User)
| similarityFunction| Defines the similarity function to be used by the model. Lift favors serendipity, Co-occurrence favors predictability, and Jaccard is a nice compromise between the two. | *Cooccurrence*, *Lift*, *Jaccard* (Lift)
| enableColdItemPlacement |  Indicates if recommendations should also push cold items via feature similarity.  | True, False (False)
| enableColdToColdRecommendations | Indicates whether the similarity between pairs of cold items (catalog items without usage) should be computed. If set to false, only similarity between cold and warm items will be computed, using catalog item features. Note that this configuration is only relevant when enableColdItemPlacement is set to true. | True, False (False)
| enableUserAffinity | For personalized recommendations, it defines whether the event type and the time of the event should be considered as input into the scoring. | True, False (False)
| enableBackfilling | Backfill with popular items when the system does not find sufficient recommendations. | True, False (True)
| allowSeedItemsInRecommendations |  Allow seed items (items in the input or in the user history) to be returned as recommendation results. | True, False (False)
| decayPeriodInDays | The decay period in days. The strength of the signal for events that are that many days old will be half that of the most recent events. | Integer (30)

> **Sample Request Body**:
>```
>{
>    "description": "Simple recommendations model",
>    "blobContainerName": "input-files",
>    "catalogFileRelativeLocation": "books.csv",
>    "usageFolderRelativeLocation": "booksTrainUsage",
>    "evaluationUsageFolderRelativeLocation": "booksTestUsage",
>    "supportThreshold": 6,
>    "cooccurrenceUnit": "User",
>    "similarityFunction": "Jaccard",
>    "enableColdItemPlacement": true,
>    "enableColdToColdRecommendations": false,
>    "enableUserAffinity": true,
>    "allowSeedItemsInRecommendations": true,
>    "enableBackfilling": true,
>    "decayPeriodInDays": 30
>}
>```

### Catalog File Schema
The catalog file contains information about the items you are offering to your customer.
The catalog data should follow the following format:

* Without features - `<Item Id>,<Item Name>,<Item Category>[,<Description>]`
* With features - `<Item Id>,<Item Name>,<Item Category>,[<Description>],<Features list>`

#### Sample Rows in a Catalog File
Without features:

    AAA04294,Office Language Pack Online DwnLd,Office
    AAA04303,Minecraft Download Game,Games
    C9F00168,Kiruna Flip Cover,Accessories

With features:

    AAA04294,Office Language Pack Online DwnLd,Office,, softwaretype=productivity, compatibility=Windows
    BAB04303,Minecraft DwnLd,Games, softwaretype=gaming,, compatibility=iOS, agegroup=all
    C9F00168,Kiruna Flip Cover,Accessories, compatibility=lumia,, hardwaretype=mobile

#### Schema details
| Name | Mandatory | Type | Description |
|:--- |:--- |:--- |:--- |
| Item Id |Yes |[A-z], [a-z], [0-9], [_] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br> Max length: 50 |Unique identifier of an item. |
| Item Name |Yes |Any alphanumeric characters<br> Max length: 255 |Item name. |
| Item Category |Yes |Any alphanumeric characters <br> Max length: 255 |Category to which this item belongs (e.g. Cooking Books, Drama…); can be empty. |
| Description |No, unless features are present (but can be empty) |Any alphanumeric characters <br> Max length: 4000 |Description of this item. |
| Features list |No |Any alphanumeric characters <br> Max length: 4000; Max number of features:20 |Comma-separated list of feature name=feature value that can be used to enhance model recommendation. |


#### Why add features to the catalog?
The recommendations engine creates a statistical model that tells you what items are likely to be liked or purchased by a customer. When you have a new product that has never been interacted with it is not possible to create a model on co-occurrences alone. Let's say you start offering a new "children's violin" in your store, since you have never sold that violin before you cannot tell what other items to recommend with that violin.

That said, if the engine knows information about that violin (i.e. It's a musical instrument, it is for children ages 7-10, it is not an expensive violin, etc.), then the engine can learn from other products with similar features. For instance, you have sold violin's in the past and usually people that buy violins tend to buy classical music CDs and sheet music stands.  The system can find these connections between the features and provide recommendations based on the features while your new violin has little usage.

Features are imported as part of the catalog data. The SAR algorithm that is used to train the model will automatically detect the strength of each of the features based on the transaction data.

#### Features are Categorical
You should create features that resemble a category. For instance, price=9.34 is not a categorical feature. On the other hand, a feature like priceRange=Under5Dollars is a categorical feature. Another common mistake is to use the name of the item as a feature. This would cause the name of an item to be unique so it would not describe a category. Make sure the features represent categories of items.

#### How many/which features should I use?
You should use less than 20 features.

#### When are features actually used?
Features are used by the model when there is not enough transaction data to provide recommendations on transaction information alone. So features will have the greatest impact on “cold items” – items with few transactions. If all your items have sufficient transaction information you may not need to enrich your model with features.

### Usage Data
A usage file contains information about how those items are used, or the transactions from your business.

#### Usage Events File Schema
A usage file is a CSV (comma separated value) file where each row in a usage file represents an interaction between a user and an item. Each row is formatted as follows:<br>
`<User Id>,<Item Id>,<Time>`

| Name | Mandatory | Type | Description |
| --- | --- | --- | --- |
| User Id |Yes |[A-z], [a-z], [0-9], [_] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br> Max length: 255 |Unique identifier of a user. |
| Item Id |Yes |[A-z], [a-z], [0-9], [&#95;] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br> Max length: 50 |Unique identifier of an item. |
| Time |Yes |Date in format: YYYY/MM/DDTHH:MM:SS (e.g. 2013/06/20T10:00:00) |The time of the event |

#### Sample Rows in a Usage File
    00037FFEA61FCA16,288186200,2015/08/04T11:02:52
    0003BFFDD4C2148C,297833400,2015/08/04T11:02:50
    0003BFFDD4C2118D,297833300,2015/08/04T11:02:40
    00030000D16C4237,297833300,2015/08/04T11:02:37
    0003BFFDD4C20B63,297833400,2015/08/04T11:02:12
    00037FFEC8567FB8,297833400,2015/08/04T11:02:04

## Get model information
*GET /api/models/\{modelId\}*

Returns metadata about the model, including the training status, parameters used to
build the model, the time of creation, statistics about the model and evaluation results.

## List models
*GET /api/models*

Return model metadata for each existing model.

## Delete a model
*DELETE /api/models/\{modelId\}*

Deletes the specified model.

If you delete the default model then there will be no default model until it is set again. Any recommendation requests to the default model will fail.

## Get item-to-item recommendations for a single item
*GET /api/models/\{modelId\}/recommend*

Gets item-to-item recommendations for the model specified.

Empty recommendations may be returned if none of the items are in the catalog or if the model
did not have sufficient training data to provide recommendations for the item.

*Request parameters*

|  Request Parameter  | Description    | Valid values (default)
|-------------|-------------------|-----------------
|  itemId     |  Seed item Id | String
| recommendationCount | Number of recommended items to return | 1-100 (10)

## Get personalized recommendations
*POST /api/models/\{modelId\}/recommend*

Gets personalized recommendations for the model specified, given a set of recent events for a particular user.
The *recent events* -- or recent history -- should be passed in the body of the request in the following format:

| Event Member | Description   | Valid values (default)
|--------------|---------------|-----------------
|    itemId    |  Seed item Id | String
|   timestamp  |  Timestamp of event | Timestamp (defaults to *UTC Now*)
|   eventType  |  Event type. This will determain the event strength **only if _weight_** is not provided  | Click (=weight of 1)<br>RecommendationClick (=weight of 2)<br>AddShopCart (=weight of 3)<br>RemoveShopCart (=weight of -1)<br>Purchase (=weight of 4)<br> (defaults to *Click*)
|    weight    |  Custom event strength. If provided, **_evnetType_ will be ignored** | float

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
