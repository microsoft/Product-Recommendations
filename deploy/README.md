# Product Recommendation Solutions deployment via ARM

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fbazzdg%2FProduct-Recommendations%2Fmaster%2Fdeploy%2Fresources.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

## Deployment Instructions

Before proceding, please ensure that you have an active Azure Subscription. If you do not, create one by visitng the [Azure Portal](https://portal.azure.com/). Once this is complete, click on the Deploy To Azure button above. The default parameters should suffice. If you wish to change them, you may go ahead and select a different value from the dropdown for the Account Type, Hosting Plan Sku, and the App Insights Location. We recommend leaving the Deploy Package Uri to its default value.

Once your deployment is complete, please return here to read the Next steps.

## Next Steps post deployment

Congratulations, the Recommendations solution has been deployed to your Azure subscription!!
You can use this service to train recommendation models and to get product recommendations.

### Deployment information

Please take note of the following pieces of information so you can use them to access the newly created RESTful endpoint that you can use to train models, and get product recommendations from
those models. 

To get the following values, you must go to the Deployment page for the deployment which you just created in the Azure Portal. This can be found in the resource group that was just created (under the deployments section). Once here, please click on Outputs to see the deployments outputs which you'll need to get the values in the table below. This screenshot should help you find the deployment outputs.

![Diagram](deploymentOutputs.PNG)

|*Output Description* |*Output parameter in Deployment Outputs*|
|-----------|----------|
|**End Point (WEBSITEURL)**|WEBSITEURL Output |
|**Admin Key (Primary)**|ADMINPRIMARYKEY Output |
|**Recommender Key** |RECOMMENDPRIMARYKEY Output |
|**Recommendations UI**|{WEBSITEURL Output}/ui |
|**Swagger**|{WEBSITEURL Output}/swagger |
|**Storage Account Connection String**|STORAGECONNECTIONSTRING Output |

&nbsp;
  
The *Admin Key* can be used for all API operations, including model creation and deletion.
The *Recommender Key* can only be used to get recommendations. This is the key you would use on the client or website requesting recommendations.

These **keys can be found and changed** in the [Azure Portal](https://portal.azure.com/), under **Application Settings** of the newly created App Service.

### Additional Resources

#### [Getting Started Guide](https://go.microsoft.com/fwlink/?linkid=847717)

Learn how to create your first model.  

#### [API Reference](https://go.microsoft.com/fwlink/?linkid=849030)
Learn about the APIs exposed by the endpoint. You can also take a look at the [Swagger interface]({Outputs.websiteUrl}/swagger) for your service.

#### [C# Sample](https://go.microsoft.com/fwlink/?linkid=847717&pc=c-sharp-sample)
Train your first model and get recommendations using the [sample code](https://go.microsoft.com/fwlink/?linkid=847717&pc=c-sharp-sample). For your convenience, you can simply replace the below code snippet in the sample to get it working with this deployment:

```
string recommendationsEndPointUri = @"{WEBSITEURL Output}";  
string apiAdminKey = @"{ADMINPRIMARYKEY Output}";
string connectionString = @"{STORAGECONNECTIONSTRING Output}";
```