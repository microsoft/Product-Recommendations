# Getting Started Guide

## Step 1: Deploy the recommendations pre-configured solution

If you have not done so already, please deploy the solution to your Azure subscription
by following the (Deployment Instructions)[deployment-instructions.md]

## Step 2: Collect Data to Train your Model
The preconfigured solution allow you to create recommendations models by learning 
from your past transactions. To train a mode you will need to provide two pieces
of information: a catalog file, and usage data.

> If you just want to use [sample data](http://aka.ms/RecoSampleData) to create your model, 
> you can download it [here](http://aka.ms/RecoSampleData). 

Below is the information on the catalog and usage file schemas:  

### Catalog file format
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

#### Format details
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

Features are imported as part of the catalog data, and then their rank (or the importance of the feature in the model) is associated when a rank build is done. Feature rank can change according to the pattern of usage data and type of items. But for consistent usage/items, the rank should have only small fluctuations. The rank of features is a non-negative number. The number 0 means that the feature was not ranked (happens if you invoke this API prior to the completion of the first rank build). The date at which the rank was attributed is called the score freshness.

#### Features are Categorical
This means that you should create features that resemble a category. For instance, price=9.34 is not a categorical feature. On the other hand, a feature like priceRange=Under5Dollars is a categorical feature. Another common mistake is to use the name of the item as a feature. This would cause the name of an item to be unique so it would not describe a category. Make sure the features represent categories of items.

#### How many/which features should I use?
Ultimately the Recommendations build supports building a model with up to 20 features. You could assign more than 20 features to the items in your catalog, but you are expected to do a ranking build and pick only the features that rank high. (A feature with a rank of 2.0 or more is a really good feature to use!). 

#### When are features actually used?
Features are used by the model when there is not enough transaction data to provide recommendations on transaction information alone. So features will have the greatest impact on “cold items” – items with few transactions. If all your items have sufficient transaction information you may not need to enrich your model with features.

### Usage Data
A usage file contains information about how those items are used, or the transactions from your business.

#### Usage Format details
A usage file is a CSV (comma separated value) file where each row in a usage file represents an interaction between a user and an item. Each row is formatted as follows:<br>
`<User Id>,<Item Id>,<Time>,[<Event>]`

| Name | Mandatory | Type | Description |
| --- | --- | --- | --- |
| User Id |Yes |[A-z], [a-z], [0-9], [_] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br> Max length: 255 |Unique identifier of a user. |
| Item Id |Yes |[A-z], [a-z], [0-9], [&#95;] &#40;Underscore&#41;, [-] &#40;Dash&#41;<br> Max length: 50 |Unique identifier of an item. |
| Time |Yes |Date in format: YYYY/MM/DDTHH:MM:SS (e.g. 2013/06/20T10:00:00) |Time of data. |
| Event |No |One of the following:<br>• Click<br>• RecommendationClick<br>•    AddShopCart<br>• RemoveShopCart<br>• Purchase |The type of transaction. |

#### Sample Rows in a Usage File
    00037FFEA61FCA16,288186200,2015/08/04T11:02:52,Purchase
    0003BFFDD4C2148C,297833400,2015/08/04T11:02:50,Purchase
    0003BFFDD4C2118D,297833300,2015/08/04T11:02:40,Purchase
    00030000D16C4237,297833300,2015/08/04T11:02:37,Purchase
    0003BFFDD4C20B63,297833400,2015/08/04T11:02:12,Purchase
    00037FFEC8567FB8,297833400,2015/08/04T11:02:04,Purchase

> ### How much data do you need?
> The quality of your model is heavily dependent on the quality and quantity of your data.
> The system learns when users buy different items (We call this co-occurrences). For FBT builds, it is also important to know which items are purchased in the same transactions.
> 
> A good rule of thumb is to have most items be in 20 transactions or more, so if you had 10,000 items in your catalog, we would recommend that you have at least 20 times that number of transactions or about 200,000 transactions. Once again, this is a rule of thumb. You will need to experiment with your data.


## Step 3: Upload Catalog and Usage Data to Blob Storage

When you train a model (step 4 in this document), you need to provide the azure storage 
location of the catalog and usage files needed for training.

### 3.1: Create a Storage Container
If you don't have one already, please [create a new Azure Storage account](https://docs.microsoft.com/en-us/azure/storage/storage-create-storage-account#create-a-storage-account).
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
 
Make sure that the usage files are all in the same virtual folder. 
For instance, the could be under a folder named *usage*.


## Step 4: Train a new Recommendations Model
Now that you have your data in Azure, we can train the model!

>  After deploying the service, you will be able to navigate to its API Reference
>  (Swagger definition) at http://\<root url\>/swagger/ .
>
>  Note that you will get the root url upon deploying your service.

We are going to use the POST /api/models call to create a new model.  
You can learn more about this API call and others in the [API Reference](api-reference.md).

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



## Step 5: Score the Model!

This is what a sample request will look like: 

```
GET http://localhost:1742/api/models/e16198c0-3a72-4f4d-b8ab-e4c07c9bccdb/recommend?itemId=6480764 HTTP/1.1
Authorization: Basic YWxscmVjaXBlcy5yZWNvQG91dGxvb2suY29tOlJTYUI4aVNsTFBnaGtoUjN2M2RBMSArIFk0eWlBWllCa25CMERNMTh1ZDRrMA==
Host: localhost:1742
Content-Length: 0
Content-Type: application/json
Accept: application/json
```

The request will look like this:



[{"recommendedItemId":"971880107","score":0.0},{"recommendedItemId":"316666343","score":0.0},{"recommendedItemId":"385504209","score":0.0},{"recommendedItemId":"60928336","score":0.0},{"recommendedItemId":"312195516","score":0.0},{"recommendedItemId":"44023722","score":0.0},{"recommendedItemId":"679781587","score":0.0},{"recommendedItemId":"142001740","score":0.0},{"recommendedItemId":"67976402","score":0.0},{"recommendedItemId":"671027360","score":0.0}]


## Step 6: A few things to consider before you go to production...