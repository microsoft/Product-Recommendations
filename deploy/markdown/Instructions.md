## Next Steps

Congratulations, the Recommendations solution has been deployed to your Azure subscription!!
You can use this service to train recommendation models and to get product recommendations.

### Deployment information

Please take note of the following pieces of information so you can use them to access the newly created RESTful endpoint that you can use to train models, and get product recommendations from
those models. 


| | |
|:---|:---|
|**End Point**| &nbsp;&nbsp;[{Outputs.websiteUrl}]({Outputs.websiteUrl}) |
|&nbsp;||
|**Admin Key**| &nbsp;&nbsp;{Outputs.adminPrimaryKey} |
|&nbsp;||
|**Recommender Key** | &nbsp;&nbsp;{Outputs.recommendPrimaryKey} |
|&nbsp;||
|**Recommendations UI**| &nbsp;&nbsp;[{Outputs.websiteUrl}/ui]({Outputs.websiteUrl}/ui) |
|&nbsp;||
|**Swagger**| &nbsp;&nbsp;[{Outputs.websiteUrl}/swagger]({Outputs.websiteUrl}/swagger) |
|&nbsp;||
|**Storage Account Connection String** | &nbsp;&nbsp;{Outputs.storageConnectionString} |

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
string recommendationsEndPointUri = @"{Outputs.websiteUrl}";  
string apiAdminKey = @"{Outputs.adminPrimaryKey}";
string connectionString = @"{Outputs.storageConnectionString}";
```