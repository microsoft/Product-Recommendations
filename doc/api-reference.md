# API Reference

## Swagger UI
Once you deploy the service, you will be able to navigate to the API Reference (Swagger definition)
of your newly created service by going to **https://<service_name>.azurewebsites.net/swagger** . You will get the root url
upon deploying your service.

The swagger UI is the authoritative definition of the API interface -- but we have duplicated a
description of the API reference here so you can understand what the service will look like before
you actually deploy it.

## Quick Reference

Use the following links to quickly jump to the relevant documentation:

| API Documentation Reference
| ---------------------------
| [Authentication](#authentication)
| [Train a New Model API](#train-a-new-model)
| [Get Model Information API](#get-model-information)
| [List Models API](#list-models)
| [Delete a Model API](#delete-a-model)
| [Get Item-To-Item Recommendations API](#get-item-to-item-recommendations)
| [Get Personalized Recommendations API](#get-personalized-recommendations)
| [Set Default Model Id API](#set-default-model-id)
| [Get Default Model Information API](#get-default-model-information)
| [Get Item-To-Item Recommendations from the Default Model API](#get-item-to-item-recommendations-from-the-default-model)
| [Get Personalized Recommendations from the Default Model API](#get-personalized-recommendations-from-the-default-model)

| Schemas Documentation Reference
| -------------------------------
| [Catalog File Schema](#catalog-file-schema)
| [Usage Events File Schema](#usage-events-file-schema)
| [Model Object Schema](#model-object-schema)
| [Model Training Parameters Schema](#model-training-parameters-schema)
| [Model Training Statistics Schema](#model-training-statistics-schema)
| [Parsing Report Schema](#parsing-report-schema)
| [Parsing Error Schema](#parsing-error-schema)
| [Model Evaluation Schema](#model-evaluation-schema)
| [Model Evaluation Metrics Schema](#model-evaluation-metrics-schema)
| [Precision Metric](#precision-metric-schema)
| [Model Diversity Metrics](#model-diversity-metrics-schema)
| [Diversity Percentile Bucket](#diversity-percentile-bucket)
| [Get Recommendations Usage Event](#get-recommendations-usage-event)
| [Recommendation Object Schema](#recommendation-object-schema)

## Authentication
When calling the service, you will need to set authentication headers with the API key that is provided as part of the set-up process.

If you did not copy the keys at the time of setting up the preconfigured solution, you can find those by:
1. Logging into the [Azure Portal](https://portal.azure.com/)
2. Navigating to the resource group that was created by the solution. The resource group is named after the deployment name you provided when you set up the solution.
3. Then navigate to **App Service**.
4. Click on **Application Settings**
5. The keys are under the **App settings** section, they are called *AdminPrimaryKey*, *AdminSecondaryKey*, *RecommendPrimaryKey* and *RecommendSecondaryKey*.

The primary and secondary *Admin* keys can be used for all API operations while the primary and secondary *Recommend* keys can only be used to 
get recommendations, typically used in client applications or websites requesting recommendations.

Sample authentication header:

```
x-api-key: <yourKeyGoesHere>
```

## Train a New Model
*POST /api/models*

Starts the process of training a new model that could be later used to query for
recommendations. To start training a model one must first upload usage data, and optionally a catalog and evaluation files,
to the new blob storage account created by the preconfigured solution.

Training a new model is an asynchronous operation. The response to this HTTP request contains a *Location* header that 
reference the newly created model, as well as the model in the body of the response.
You can use the [Get Model API](#get-model-information) to query for the model training status along with other model information.
 
Optionally model metrics are computed if *evaluationUsageRelativePath* is provided. See [Model Evaluation](model-evaluation.md) for more details.

The **request** body should be a valid [Model Training Parameters](#model-training-parameters-schema) JSON object.

The **response** body will contain a [Model](#model-object-schema) JSON object.

> **Sample Request:**
>```
>POST https://<service_name>.azurewebsites.net/api/models
>x-api-key: your_api_key
>Content-Type: application/json
>
>{
>    "description": "Simple recommendations model",
>    "blobContainerName": "input-files",
>    "usageRelativePath": "booksTrainUsage",
>    "catalogFileRelativePath": "books.csv",
>    "evaluationUsageRelativePath": "booksTestUsage",
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

> **Sample Response:**
>```
>HTTP/1.1 201 Created
>Content-Type: application/json; charset=utf-8
>Location: https://<service_name>.azurewebsites.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb
>
>{
>    "id":"e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb",
>    "description": "Simple recommendations model",
>    "creationTime":"2017-05-04T00:26:06.386324Z",
>    "modelStatus":"Created"
>}
>```

If you would like to train a "Frequently Bought Together" model:

-	Set “enableUserAffinity” to false since you don’t need the time of the event or the event type to be used as inputs into the recommendation request.
-	Set the “supportThreshold” based on the minimum number of co-occurrences that you want items to appear together in a transaction to be used in the model.
-	Set “cooccurrenceUnit”. You can set to “Timestamp” if you want baskets to be modelled based on transactions that occurred at the same time in the check-out. If that is too strict, you can set to “User”.
-	Set the “similarityFunction” desired. We would recommend starting with Jaccard. 


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
| Item Id |Yes | [A-z], [a-z], [0-9], [_] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br/> Max length: 450 |Unique identifier of an item. |
| Item Name |Yes |Any alphanumeric characters<br/> Max length: 255 |Item name. |
| Item Category |Yes |Any alphanumeric characters <br/> Max length: 255 |Category to which this item belongs (e.g. Cooking Books, Drama…); can be empty. |
| Description |No, unless features are present (but can be empty) |Any alphanumeric characters <br/> Max length: 4000 |Description of this item. |
| Features list |No |Any alphanumeric characters <br/> Max length: 4000; Max number of features:20 |Comma-separated list of feature name=feature value that can be used to enhance model recommendation. |


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
A usage file is a CSV (comma separated value) file where each row in a usage file represents an interaction between a user and an item. Each row is formatted as follows:<br/>
`<User Id>,<Item Id>,<Time>[,<Event Type> | ,,<Custom Event Weight>]`

| Name | Mandatory | Type | Description |
| --- | --- | --- | --- |
| User Id |Yes |[A-z], [a-z], [0-9], [_] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br/> Max length: 255 |Unique identifier of a user. |
| Item Id |Yes |[A-z], [a-z], [0-9], [&#95;] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br/> Max length: 450 |Unique identifier of an item. |
| Time |Yes |Date in ISO 8601 format:<br/>**yyyy-MM-ddTHH:mm:ss**<br/>(e.g. 2017-04-20T18:00:00) |The time of the event |
| Event Type<br/><br/><br/>*only used in [model evaluation](#model-evaluation.md) |No |One of the following:<br/> **Click** (=weight of 1)<br/>**RecommendationClick** (=weight of 2)<br/>**AddShopCart** (=weight of 3)<br/>**RemoveShopCart** (=weight of -1)<br/>**Purchase** (=weight of 4)<br/> (defaults to **Click**)|The type of transaction. <br/>This will be used to determine the event strength. <br/><br/> *used only if *Custom Event Weight* is not provided. | 
| Custom Event Weight <br/><br/>*only used in [model evaluation](#model-evaluation.md)|No |number | The trasaction strength.<br/><br/>*if provided, *Event Type* will be ignored. | 

#### Sample Rows in a Usage File
    00037FFEA61FCA16,288186200,2015-08-14T11:02:52,Click 
    0003BFFDD4C2148C,297833400,2015-08-14T11:02:50,Purchase 
    0003BFFDD4C2118D,297833300,2015-08-14T11:02:40,,5.2 
    00030000D16C4237,297833300,2015-08-14T11:02:37,Purchase 
    0003BFFDD4C20B63,297833400,2015-08-14T11:02:12,Click 
    00037FFEC8567FB8,297833400,2015-08-14T11:02:04

## Get Model Information
*GET /api/models/\{modelId\}*

Returns the model information, including the training status, parameters used to
build the model, the time of creation, statistics about the model and evaluation results
(See [Model JSON Schema](#model-object-schema)).


> **Sample Request:**
>```
>GET https://<service_name>.azurewebsites.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb
>x-api-key: your_api_key
>```
> **Sample Response:**
>```
>HTTP/1.1 200 OK
>Content-Type: application/json; charset=utf-8
>
>{
>    "id":"e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb",
>    "description": "Simple recommendations model",
>    "creationTime":"2017-05-04T00:26:06.386324Z",
>    "modelStatus": "Completed",
>    "modelStatusMessage": "Model Training Completed Successfully",
>    "parameters": {
>        ...
>    },
>    "statistics": {
>        ...
>    }
>}
>```

## List Models
*GET /api/models*

Returns the id, description, creation time and status of all the existing models. (See [Model JSON Schema](#model-object-schema)).

> **Sample Request:**
>```
>GET https://<service_name>.azurewebsites.net/api/models
>x-api-key: your_api_key
>```
> **Sample Response:**
>```
>HTTP/1.1 200 OK
>Content-Type: application/json; charset=utf-8
>
>[{
>    "id":"e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb",
>    "description": "Simple recommendations model",
>    "creationTime":"2017-05-04T00:26:06.386324Z"
>    "modelStatus": "Completed"
>},
>{
>    "id": "1bd76c6d-0a25-4d9a-8111-9e897823ae1f",
>    "description": "my other model",
>    "creationTime": "2017-05-04T00:26:51.1024762Z",
>    "modelStatus": "Created"
>}]
>```

## Delete a Model
*DELETE /api/models/\{modelId\}*

Deletes the specified model.

> **Important** If you delete the default model then there will be no default model until it is set again. Any recommendation requests to the default model **will fail**.

> **Sample Request:**
>```
>DELETE https://<service_name>.azurewebsites.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb
>x-api-key: your_api_key
>```
> **Sample Response:**
>```
>HTTP/1.1 202 Accepted
>Content-Length: 0
>```

## Get Item-To-Item Recommendations
*GET /api/models/\{modelId\}/recommend*

Gets item-to-item recommendations for the model specified.

Empty recommendations may be returned if none of the items are in the catalog or if the model
did not have sufficient training data to provide recommendations for the item.

*Request parameters*

|  Request Parameter  | Description    | Valid Values | Default Value
|-------------|-------------------|-----------------|-----------------
|  itemId     |  Seed item Id | string
| recommendationCount | Number of recommended items to return | 1-100 | 10


The response body will be a JSON array of [Recommendation Objects](#recommendation-object-schema).

> **Sample Request:**
>```
>GET https://<service_name>.azurewebsites.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb/recommend?itemId=70322
>x-api-key: your_api_key
>```
> **Sample Response:**
>
>```
>HTTP/1.1 200 OK
>Content-Type: application/json; charset=utf-8
>
>[{
>	"recommendedItemId": "46846",
>	"score": 0.45787626504898071
>},
>{
>	"recommendedItemId": "46845",
>	"score": 0.12505614757537842
>},
>...
>{
>	"recommendedItemId": "41607",
>	"score": 0.049780447036027908
>}]
>```

## Get Personalized Recommendations
*POST /api/models/\{modelId\}/recommend*

Gets personalized recommendations for the model specified, given a set of recent usage events for a particular user. 

An optional user id may also be specified, in which case the usage events of that user, extracted during model training from the input usage files, will also be considered.
> Note: If a user id is provided, additional usage events may still be provided in the request body, representing a more recent user activity

*Request parameters*

|  Request Parameter  | Description                                  | Valid Values | Default Value
|---------------------|----------------------------------------------|--------------|-----------------
|  userId             | The id of a user to get recommendations for. The user id will be ignored unless the model was trained with a *enableUserToItemRecommendations* set to **true**.<br/>See [Model Training Parameters Schema](#model-training-parameters-schema) for more info. | string       | null
| recommendationCount | Number of recommended items to return        | 1-100        | 10


The request body must be an array of [Get Recommendations Usage Events](#get-recommendations-usage-event).

The response body will be a JSON array of [Recommendation Objects](#recommendation-object-schema).

> **Sample Request:**
>```
>POST https://<service_name>.azurewebsites.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb/recommend?userId=user032669023
>x-api-key: your_api_key
>
>[
>    {
>       "itemId": "ItemId123",
>       "eventType": "Click",
>       "timestamp": "2017-01-31T23:59:59"
>    },
>    {
>       "itemId": "ItemId456",
>       "eventType": "Purchase"
>    },
>    {
>       "itemId": "ItemId789",
>       "weight": 2.3
>    },
>    {
>       "itemId": "ItemId135"
>    }
>]
>```
> **Sample Response:**
>
>```
>HTTP/1.1 200 OK
>Content-Type: application/json; charset=utf-8
>
>[{
>	"recommendedItemId": "46846",
>	"score": 0.45787626504898071
>},
>{
>	"recommendedItemId": "46845",
>	"score": 0.12505614757537842
>},
>...
>{
>	"recommendedItemId": "41607",
>	"score": 0.049780447036027908
>}]
>```

## Set Default Model Id
*PUT /api/models/default*

Sets the specified model id as the default model.

*Request parameters:*

|  Request Parameter  | Description    | Valid Value
|-------------|-------------------|-----------------
|  modelId     |  The model id to set as the default model | GUID

> **Sample Request:**
>```
>PUT https://<service_name>.azurewebsites.net/api/models/default?modelId=e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb
>x-api-key: your_api_key
>```
> **Sample Response:**
>
>```
>HTTP/1.1 200 OK
>```

## Get Default Model Information
*GET /api/models/default* 

> To use this API, a default model must first be set

Returns the default model information, including the training status, parameters used to
build the model, the time of creation, statistics about the model and evaluation results
(See [Model JSON Schema](#model-object-schema)).

> **Sample Request:**
>```
>GET https://<service_name>.azurewebsites.net/api/models/default
>x-api-key: your_api_key
>```
> **Sample Response:**
>```
>HTTP/1.1 200 OK
>Content-Type: application/json; charset=utf-8
>
>{
>    "id":"e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb",
>    "description": "Simple recommendations model",
>    "creationTime":"2017-05-04T00:26:06.386324Z",
>    "modelStatus": "Completed",
>    "modelStatusMessage": "Model Training Completed Successfully",
>    "parameters": {
>        ...
>    },
>    "statistics": {
>        ...
>    }
>}
>
>```

## Get Item-To-Item Recommendations from the Default Model
*GET /api/models/default/recommend*

> To use this API, a default model must first be set

Gets item-to-item recommendations for the *default* model.

Empty recommendations may be returned if none of the items are in the catalog or if the model
did not have sufficient training data to provide recommendations for the item.

*Request parameters*

|  Request Parameter  | Description    | Valid Values | Default Value
|-------------|-------------------|-----------------|-----------------
|  itemId     |  Seed item Id | string
| recommendationCount | Number of recommended items to return | 1-100 | 10


The response body will be a JSON array of [Recommendation Objects](#recommendation-object-schema).
*Request parameters*

|  Request Parameter  | Description    | Valid Values | Default Value
|-------------|-------------------|-----------------|-----------------
|  itemId     |  Seed item Id | string
| recommendationCount | Number of recommended items to return | 1-100 | 10


> **Sample Request:**
>```
>GET https://<service_name>.azurewebsites.net/api/models/default/recommend?itemId=70322
>x-api-key: your_api_key
>```
> **Sample Response:**
>
>```
>HTTP/1.1 200 OK
>Content-Type: application/json; charset=utf-8
>
>[{
>	"recommendedItemId": "46846",
>	"score": 0.45787626504898071
>},
>{
>	"recommendedItemId": "46845",
>	"score": 0.12505614757537842
>},
>...
>{
>	"recommendedItemId": "41607",
>	"score": 0.049780447036027908
>}]
>```

## Get Personalized Recommendations from the Default Model
*POST /api/models/default/recommend*

> To use this API, a default model must first be set

Gets personalized recommendations for the *default* model, given a set of recent usage events for a particular user.
The request body should be an array of [Get Recommendations Usage Events](#get-recommendations-usage-event).
The response body will be a JSON array of [Recommendation Objects](#recommendation-object-schema).

> **Sample Request:**
>```
>POST https://<service_name>.azurewebsites.net/api/models/default/recommend
>x-api-key: your_api_key
>
>[
>    {
>       "itemId": "ItemId123",
>       "eventType": "Click",
>       "timestamp": "2017-01-31T23:59:59"
>    },
>    {
>       "itemId": "ItemId456",
>       "eventType": "Purchase",
>    },
>    {
>       "itemId": "ItemId789",
>       "weight": 2.3,
>    },
>    {
>       "itemId": "ItemId135",
>    }
>]
>```
> **Sample Response:**
>
>```
>HTTP/1.1 200 OK
>Content-Type: application/json; charset=utf-8
>
>[{
>	"recommendedItemId": "46846",
>	"score": 0.45787626504898071
>},
>{
>	"recommendedItemId": "46845",
>	"score": 0.12505614757537842
>},
>...
>{
>	"recommendedItemId": "41607",
>	"score": 0.049780447036027908
>}]
>```

## Model Object Schema

The following table specifies the schema of the *model* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| id | string | The model id
| description | string | A textual description of the model, as provided when the model was created
| creationTime | Date Time |  The model creation UTC time and date
| modelStatus | string | The status of the model:<br/>**Created** - the initial status of a new model<br/>**InProgress** - the model is being trained<br/>**Completed** - model trainining was completed successfully<br/>**Failed** - model trainining had failed
| modelStatusMessage | string | An optional message associated with the model status
| parameters | [Model Training Parameters](#model-training-parameters-schema) | The parameters used for training the model, as provided when the model was created
| statistics | [Model Training Statistics](#model-training-statistics-schema) | The model training statistics, specifying metrics like training duration and model quality

## Model Training Parameters Schema 

The following table specifies the schema of the *model training parameters* JSON object (**mandatory properties in bold**):

| Property Name | Mandatory? | Description | Type | Default Value
|---------------|------------|-------------|--------------|--------------
| description | no | Textual description | string with max length of 256 characters | *null*
| **blobContainerName**| **yes** |  Name of the container where the catalog, usage data and evaluation data are stored. Note that this container must be in the storage account created by the preconfigured solution | string  | 
| **usageRelativePath** | **yes** |   Relative path to either a virtual directory that contains the usage file(s) or a specific usage file to be used for training. See [Usage events file format](#usage-events-file-schema) | string |
| catalogFileRelativePath | no |   Relative path to the catalog file. See [Catalog file format](#catalog-file-schema) | string  | *null*
| evaluationUsageRelativePath | no |   Relative path to either a virtual directory that contains the usage file(s) or to a specific usage file to be used for evaluation. See [Usage events file format](#usage-events-file-schema) | string | *null*
| supportThreshold | no |  How conservative the model is. Number of co-occurrences of items to be considered for modeling. | 3-50 | 6
| cooccurrenceUnit | no |  Indicates how to group usage events before counting co-occurrences. A 'User' cooccurrence unit will consider all items purchased by the same user as occurring together in the same session. A 'Timestamp' cooccurrence unit will consider all items purchased by the same user in the same time as occurring together in the same session. | *User*, *Timestamp* | User
| similarityFunction| no |  Defines the similarity function to be used by the model. Lift favors serendipity, Co-occurrence favors predictability, and Jaccard is a nice compromise between the two. | *Cooccurrence*, *Lift*, *Jaccard* | Jaccard
| enableColdItemPlacement | no |   Indicates if recommendations should also push cold items via feature similarity.  | True, False | False
| enableColdToColdRecommendations | no |  Indicates whether the similarity between pairs of cold items (catalog items without usage) should be computed. If set to false, only similarity between cold and warm items will be computed, using catalog item features. Note that this configuration is only relevant when enableColdItemPlacement is set to true. | True, False | False
| enableUserAffinity | no |  For personalized recommendations, it defines whether the event type and the time of the event should be considered as input into the scoring. | True, False | False
| enableBackfilling | no |  Backfill with popular items when the system does not find sufficient recommendations. | True, False | True
| allowSeedItemsInRecommendations | no |   Allow seed items (items in the input or in the user history) to be returned as recommendation results. | True, False | False
| decayPeriodInDays | no |  The decay period in days. The strength of the signal for events that are that many days old will be half that of the most recent events. | Integer | 30
| enableUserToItemRecommendations | no |  If true, userId is honored when requesting personalized recommendations. Training takes a bit longer when this is enabled.  | True, False | False



## Model Training Statistics Schema

The following table specifies the schema of the *model training statistics* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| totalDuration | time span | The total duration of the model training process
| trainingDuration | time span | The duration of the core model training
| catalogParsing | [Parsing Report Schema](#parsing-report-schema) |  The catalog file parsing report
| usageEventsParsing | [Parsing Report Schema](#parsing-report-schema) |  The usage events file(s) parsing report
| numberOfCatalogItems | number | The total number of items found in the catalog file
| numberOfUsers | number | The total number of unique users found in the usage events file(s)
| numberOfUsageItems | number | The total number of valid (which are present in catalog if provided) unique items found in usage file(s)
| catalogCoverage | number | The ratio of unique items found in usage file(s) and total items in catalog
| evaluation | [Model Evaluation Schema](#model-evaluation-schema) | The model evaluation metrics
| catalogFeatureWeights | [Catalog Feature Weights Schema](#catalog-feature-weights-schema) | The calculated catalog feature's weights

## Parsing Report Schema

The following table specifies the schema of the *catalog\usage file(s) parsing report* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| duration | time span | The total parsing duration
| errors | An array of [Parsing Error Schema](#parsing-error-schema) | A list of line parsing errors, if found
| successfulLinesCount | number | The total number of lines parsed
| totalLinesCount | number | The number of items with an unknown id

## Parsing Error Schema

The following table specifies the schema of the *parsing error* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| error | string | The type of the parsing error: <br/>**MalformedLine** - The line is in an invalid CSV format<br/>**MissingFields** - The line is missing some mandatory fields<br/>**BadTimestampFormat** - The time stamp field is malformed<br/>**BadWeightFormat** - The event weight field is not numeric<br/>**MalformedCatalogItemFeature** - Some catalog item feature has a malformed format<br/>**ItemIdTooLong** - The item id string is longer than the maximum allowed<br/>**IllegalCharactersInItemId** - The item id string contains invalid characters.<br/>**UserIdTooLong** - The user id string is longer than the maximum allowed<br/>**IllegalCharactersInUserId** - The user id string contains invalid characters.<br/>**UnknownItemId** - The item id doesn't appear in the catalog<br/>**DuplicateItemId** - The item id appears more than once in the catalog<br/>
| count | number | The number of occurrences of this particular error
| sample | [Parsing Error Sample Schema](#parsing-error-sample-schema) | A sample of an occurrence of this particular error

## Parsing Error Sample Schema

| Property Name | Type | Description |
|---------------|------|-------------|
| file | string | The name of the file containing the parsing error
| line | number | The line number of the parsing error

## Model Evaluation Schema

The following table specifies the schema of the *model evaluation* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| duration | time span | The total duration of the model evaluation process
| usageEventsParsing | [Parsing Report Schema](#parsing-report-schema) |  The evaluation usage events file(s) parsing report
| metrics | [Model Evaluation Metrics Schema](#model-evaluation-metrics-schema) | The model evaluation metrics

## Model Evaluation Metrics Schema

The following table specifies the schema of the *model evaluation metrics* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| precisionMetrics | Array of [Precision Metric](#precision-metric-schema) objects | Precision@K metrics for a model. These are a measure of quality of the Model. It works by splitting the input data into a test and training data. Then use the test period to evaluate what percentage of the customers would have actually clicked on a recommendation if k recommendations had been shown to them given their prior history.
| diversityMetrics | [Model Diversity Metrics](#model-diversity-metrics-schema) | The model diversity metrics. Diversity gives customers a sense of how diverse the item recommendations are, based on their usage shown by bucket eg: 0-90, 90-99, 99-100. In simple terms, how many recommendations are coming from most popular items, how many from non-popular etc., unique items recommended.

## Precision Metric Schema

The following table specifies the schema of the *model precision metric* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| k | number | The value K used to calculate the metric values
| percentage | number | Precision@K percentage
| usersInTest | number | The total number of users found in the test dataset

## Model Diversity Metrics Schema

The following table specifies the schema of the *model diversity metric* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| percentileBuckets | Array of [Diversity Percentile Bucket](#diversity-percentile-bucket) | The diversity metrics for all of the popularity buckets
| totalItemsRecommended | number | Total number of items recommended (some may be duplicates)
| uniqueItemsRecommended | number | The total number of distinct items that were returned for evaluation
| uniqueItemsInTrainSet | The total number of distinct items in the train dataset 

## Diversity Percentile Bucket

The following table specifies the schema of the *model diversity percentile bucket* JSON object:

| Property Name | Type | Description |
|---------------|------|-------------|
| min | number | The beginning percentile of the popularity bucket (inclusive)
| max | number | The ending percentile of the popularity bucket (exclusive)
| percentage | number | The fraction of all recommended users that belong to the specified popularity bucket

## Catalog Feature Weights Schema

The following table specifies the schema of the *catalog feature weights* JSON object:

| Property Name  | Type   | Description |
|----------------|--------|-------------|
| feature name   | string | Feature name as appeared in the catalog items features. See [Feature List Schema](#schema-details)
| feature weight | number | The calculated weight of the feature. Features with higher **absolute** value indicate greater significance in the model when determining cold items correlations (cold items are catalog items that have no usage events). Negative weights indicate a reverse correlation, i.e. items that share the same value of a feature with a negative weight are considered less correlated

## Get Recommendations Usage Event

The following table specifies the schema of the *usage event* JSON object used in get recommendations requests:

| Property Name | Type | Description | Default Value
|---------------|------|-------------|-----------------
| **itemId** | string | An item id to get recommendations for | 
| timestamp |  ISO 8601 format:<br/>**yyyy-MM-ddTHH:mm:ss**<br/> | The timestamp of event | Current UTC date and time
| eventType | One of the following string values:<br/>**Click** (=weight of 1)<br/>**RecommendationClick** (=weight of 2)<br/>**AddShopCart** (=weight of 3)<br/>**RemoveShopCart** (=weight of -1)<br/>**Purchase** (=weight of 4)<br/> | The event type. This will determain the event strength **only if _weight_** is not provided | Click
| weight | number | Custom event strength. If provided, **_eventType_ will be ignored** | 1.0

## Recommendation Object Schema

The following table specifies the schema of the *recommendation* JSON object:

| Property Name | Type | Description 
|---------------|------|------------
| recommendedItemId | string | The recommended item id
| score | number | The score of this recommendation
