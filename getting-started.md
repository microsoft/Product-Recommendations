# Getting Started Guide

## Step 1: Deploy the product recommendations solution

If you have not done so already, please deploy the solution to your Azure subscription
by following the [Deployment Instructions](deploy#product-recommendation-solutions-deployment-via-arm).

## Step 2: Collect Data to Train your Model
The solution allows you to create recommendations models by learning 
from your past transactions. To train a model you will need to provide two pieces
of information: a catalog file, and usage data.

The [catalog schema](doc/api-reference.md#catalog-file-schema) and [usage events schema](doc/api-reference.md#usage-events-file-schema) could be found in the [API reference](doc/api-reference.md).

You can also checkout the [sample data](http://aka.ms/RecoSampleData).

> ### How much data do you need?
> The quality of your model is heavily dependent on the quality and quantity of your data.
> The system learns when users buy different items (We call this co-occurrences). For FBT training, it is also important to know which items are purchased in the same transactions.
> 
> A good rule of thumb is to have most items be in 20 transactions or more, so if you had 10,000 items in your catalog, we would recommend that you have at least 20 times that number of transactions or about 200,000 transactions. Once again, this is a rule of thumb. You will need to experiment with your data.

## Step 3: Upload Catalog and Usage Events Data to a Blob Container

When you train a model ([step 4](#step_4_train_a_new_recommendations_model)), you need to provide the relative paths of the catalog file and usage folder (or file) needed for training.

There are multiple ways of creating Azure blob containers (and uploading files to it). One popular option is using [Microsoft Azure Storage Explorer](http://storageexplorer.com/).
Alternatively, you could create containers and upload files directly from the Azure portal, although the experience may be less comfortable. 

### 3.1: Create a Storage Container
You will need to create an Azure storage blob container to host the usage\catalog files required for training. 
The container should be created in the Azure Storage Account associated with your service.

You may choose any name for your container, but it is recommended to avoid the name **models** as this is the name of the blob container
used by the service to store already trained model binaries.


### 3.2 Upload Usage Events File(s)

Model training requires at least one usage events file, but you may provide multiple files for your convenience.

**All usage events files must be placed in a flat structure under some folder**. No other files should be in that folder.

The usage events folder may be placed directly under the container or in some sub folder.   
For example, the usage events file(s) could be under a folder named *usage*. 

>The usage event file(s) folder (or file) path (relative to the container) is a required parameters for the train model API

### 3.3 Upload a catalog file (optional)
If you choose to use a catalog file when training, the catalog file will need to be uploaded to any place under the container created in the previous step.

The catalog file could be placed directly under the container, or in some sub folder.

>The catalog **file** path (relative to the container) is a optional parameters for the train model API 

## Step 4: Train a new Recommendations Model
Now that you have your data in the blob container, we can train the model!

>  After deploying the service, you will be able to navigate to its API Reference
>  (Swagger definition) at https://<service_name>.azurewebsites.net/swagger/ .
>
>  Note that you will get the URL upon deploying your service.

To train the model, we will call the *POST /api/models* API to create a new model. See full API spec and examples in the [train a new model API reference](doc/api-reference.md#train-a-new-model).

## Step 4: Wait for Model Training Completion

Note that the model id is provided to you when you train a new model.
You can get the status of any model by calling the reference returned in the *location* header of the response, for example:
```
GET https://<service_name>.azurewebsites.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb
```
Once the model status is *Completed*, the model will be ready for use, i.e. get recommendations. 
```
{"id":"e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb","status":"Completed"}
```

Learn more in the [Get Model Information API reference](doc/api-reference.md#get-model-information).

## Step 5: Getting Recommendations

If you want to get item-to-item recommendations, you can perform a simple GET request like described in the one below: 

```
GET https://<service_name>.azurewebsites.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb/recommend?itemId=70322
x-api-key: your_api_key
Content-Type: application/json
```

The response will look something like this:

```
[{
	"recommendedItemId": "46846",
	"score": 0.45787626504898071
},
{
	"recommendedItemId": "46845",
	"score": 0.12505614757537842
},
...
{
	"recommendedItemId": "41607",
	"score": 0.049780447036027908
}]
```

You can learn more about this API call use this [API Reference](doc/api-reference.md#get-item-to-item-recommendations).

If you want to get personalized recommendations you will need to pass the list of recent transactions for a particular user as part of the body. 
For instance, the request below is for getting recommendations for a customer that purchased item 316569321 in February, and then clicked item 6480764 in March.

```
POST https://<service_name>.azurewebsites.net/swagger/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb/recommend HTTP/1.1
x-api-key: your_api_key
Host: www.myrecommendations.net
Content-Length: 330
Content-Type: application/json
Accept: application/json

 [ 
         {          
             "itemId": "6480764",
             "type" : "click",    
             "timestamp" : "2014/03/15T10:50:00"
         } ,
         {          
             "itemId": "316569321",
             "type" : "purchase",
             "timestamp" : "2014/02/15T12:30:00"      
         }

 ]

```

For more information on getting recommendations, and to understand all options available to you, please check the [Get Personalized Recommendations API](doc/api-reference.md#get-personalized-recommendations)

## Step 6: A few things to help you go to production...

|||
|:-|:-|
|[API Reference](doc/api-reference.md) | Guide on all the APIs and their usage.|
|[Model Evaluation](doc/model-evaluation.md)| Gain insights on your model.|
|[Benchmarks](doc/benchmarks.md)| Training duration and Scoring latencies on common datasets.|
|[Troubleshooting and FAQ ](doc/troubleshooting-and-faq.md)| Guide to debugging common scenarios.|
|[Service Architecture](doc/architecture.md)| Detailed description on the service architecture. | 
|[SAR - Recommendation Algorithm](doc/sar.md)| Detailed description on the recommendation algorithm.| 



