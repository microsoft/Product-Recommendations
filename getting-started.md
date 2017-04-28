# Getting Started Guide

## Step 1: Deploy the recommendations pre-configured solution

If you have not done so already, please deploy the solution to your Azure subscription
by following the [Deployment Instructions](doc/deployment-instructions.md).

## Step 2: Collect Data to Train your Model
The preconfigured solution allows you to create recommendations models by learning 
from your past transactions. To train a model you will need to provide two pieces
of information: a catalog file, and usage data.

> If you just want to use [sample data](http://aka.ms/RecoSampleData) to create your model, 
> you can download it [here](http://aka.ms/RecoSampleData). 

The [catalog schema](doc/api-reference.md#catalog-file-schema) and [usage events schema](doc/api-reference.md#usage-events-file-schema) could be found in the [API reference](doc/api-reference.md)  


> ### How much data do you need?
> The quality of your model is heavily dependent on the quality and quantity of your data.
> The system learns when users buy different items (We call this co-occurrences). For FBT builds, it is also important to know which items are purchased in the same transactions.
> 
> A good rule of thumb is to have most items be in 20 transactions or more, so if you had 10,000 items in your catalog, we would recommend that you have at least 20 times that number of transactions or about 200,000 transactions. Once again, this is a rule of thumb. You will need to experiment with your data.


## Step 3: Upload Catalog and Usage Data to Blob Storage

When you train a model (step 4 in this document), you need to provide the azure storage 
location of the catalog and usage files needed for training.

### 3.1: Create a Storage Container (if you don't have one already)
If you don't have one already, please [create an Azure Storage account](https://docs.microsoft.com/en-us/azure/storage/storage-create-storage-account#create-a-storage-account).
You can do this from the Azure portal.

Once you have an account you will need to create a new Blob Container if you don't have one already. 

### 3.2 Generate a Shared Access Token for the container.
While you can do this programmatically, the easiest way to do this is by using  [Microsoft Azure Storage Explorer](http://storageexplorer.com/).

Simply right click on the container where you will store your files, and select *Get Shared Access Signature...*.
Make sure Read and List permissions are provided, and that the Start and Expiry dates are appropriate.

*Copy* the URL generated, you will need it in the next step.

It should looks something like this: 
```
 https://myreco.blob.core.windows.net/input-files?st=2017-03-23T23%3A45%3A00Z&se=2017-03-24T23%3A45%3A00Z&sp=rl&sv=2015-12-11&sr=c&sig=i3Tu6WQUbOTtjn7RGi99DdlaBfWoikiOzfpf2njecKU%3D
```

### 3.3 Upload catalog file and usage files.
To upload your files to Azure Blob Storage, you may do it programmatically, or use a tool 
like [Microsoft Azure Storage Explorer](http://storageexplorer.com/).

**Place all usage files in the same virtual folder.** For instance, they could be under a folder named *usage*. 
No other files should be in that folder. The catalog file can be placed anywhere else (including directly in the container).


## Step 4: Train a new Recommendations Model
Now that you have your data in Azure, we can train the model!

>  After deploying the service, you will be able to navigate to its API Reference
>  (Swagger definition) at http://\<root url\>/swagger/ .
>
>  Note that you will get the root url upon deploying your service.

We are going to use the POST /api/models call to create a new model.  
You can learn more about this API call and others in the [API Reference](doc/api-reference.md).

This is what a sample request will look like: 

```
POST http://www.myrecommendations.net/api/models HTTP/1.1
Authorization: Basic modelKeyGoesHere
Host: localhost:1742
Content-Length: 658
Content-Type: application/json
Accept: application/json

{
"description": "Simple recommendations model",
"baseContainerSasUri": "https://luiscareco.blob.core.windows.net/input-files?st=2017-03-14T11%3A07%3A00Z&se=2018-03-16T11%3A07%3A00Z&sp=rwdl&sv=2015-12-11&sr=c&sig=ztvAyzQqUCPLQ7gDGGz9x0pNnfu8TKD9HSG5EDp9C0w%3D",
"trainCatalogFileRelativeLocation": "books.csv",
"trainUsageFolderRelativeLocation": "booksusage",
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

The response of this request will look like this:

```
HTTP/1.1 201 Created
Cache-Control: no-cache
Pragma: no-cache
Content-Type: application/json; charset=utf-8
Expires: -1
Location: http://www.myrecommendations.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb
Server: Microsoft-IIS/10.0
X-AspNet-Version: 4.0.30319
X-SourceFiles: =?UTF-8?B?Qzpcc3JjXFJlY29tbWVuZGF0aW9uQ29yZVxzb3VyY2VcUmVjb21tZW5kYXRpb25zLldlYkFwcFxhcGlcbW9kZWxz?=
X-Powered-By: ASP.NET
Date: Fri, 24 Mar 2017 20:00:34 GMT
Content-Length: 64

{"id":"e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb","status":"Created"}
```

For more information on training a recommendations model, and to understand all build parameters available to you, please check the [API Reference](doc/api-reference.md).

## Step 4: Wait for your build to complete

Note that the model Id is provided to you when you train a new model.
You can the status of that model by calling the reference that is provided to you in the Location Header, in the example above:
```
GET http://www.myrecommendations.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb
```
One the status is completed as in the response below, you will be ready to use the trained model.
```
{"id":"e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb","status":"Completed"}
```

## Step 5: Now let's get some recommendations!

If you want to get item-to-item recommendations, you can perform a simple GET request like the one below. 

```
GET http://localhost:1742/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb/recommend?itemId=6480764 HTTP/1.1
Authorization: Basic YWxscmVjaXBlcy5yZWNvQG91dGxvb2suY29tOlJTYUI4aVNsTFBnaGtoUjN2M2RBMSArIFk0eWlBWllCa25CMERNMTh1ZDRrMA==
Host: localhost:1742
Content-Length: 0
Content-Type: application/json
Accept: application/json
```

The response will look something like this:

```
[
    {
        "recommendedItemId": "971880107",
        "score": 0
    },
    {
        "recommendedItemId": "316666343",
        "score": 0
    },
    {
        "recommendedItemId": "385504209",
        "score": 0
    },
  ...
    {
        "recommendedItemId": "142001740",
        "score": 0
    },
    {
        "recommendedItemId": "671027360",
        "score": 0
    }
]
```

If you want to get personalized recommendations you will need to pass the list of recent transactions for a particular user as part of the body. For instance, the RAW request below
is requesting for recommendations for a customer that purchased item 316569321 in Febrary, and then clicked item 6480764 in March.

```
POST http://www.myrecommendations.net/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb/recommend HTTP/1.1
Authorization: Basic YWxscmVjaXBlcy5yZWNvQG91dGxvb2suY29tOlJTYUI4aVNsTFBnaGtoUjN2M2RBMSArIFk0eWlBWllCa25CMERNMTh1ZDRrMA==
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

For more information on getting recommendations, and to understand all options available to you, please check the [API Reference](doc/api-reference.md).

## Step 6: A few things to consider before you go to production...

Before you go, these are a few additional thing you may want to know.

**TODO** This section is currently empty