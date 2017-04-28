![Debugging](../images/debugging.gif)

---

To make the debugging process easier, necessary telemetry has been added throughout the source code. By default, diagnostics is disabled in azure web apps and can be enabled by following the steps here - [Enable diagnostics logging for web apps in Azure App Service](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-enable-diagnostic-log). The logs should give a fair idea of where the system is failing. Below are some common scenarios to help get started.

**Tip:** Stream *logs and console output* directly from azure portal. More details are explained here - [Streaming Logs and the Console](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-streaming-logs-and-console).

---

### Common Scenarios

1. ##### API call fails with 5xx status codes
    When using _admin_ key, a detailed exception message along with inner exceptions are returned to help understand the potential problem. Note that this stack is not returned with the _recommend_ key.

2. ##### API call fails with 401 Unauthorized
   1. Ensure you are passing the api key header - "x-api-key:<*key*>".
   2. For all non recommendation APIs, ensure you are using *adminKey*.

3. ##### Model training request fails with 400 status codes
    In most cases, a clear error message indicating the issue is returned. 
    ![400Withmessage](../images/400withmessage.png)
    In some cases, error message might not point to exact point of error. This happens mainly if the body is not properly composed. Eg: a boolean parameter has a value other than true/false.
 
    ![400Withoutmessage](../images/400withoutmessage.png)

4. ##### API call fails with 415 Unsupported Media type on Post requests
    Ensure the header - "Content-Type: application/json" is set.

5. ##### Model takes forever to train. Eventually fails with status "aborted after n attempts"
    The recommendations algorithm is a memory based algorithm. Based on the [App Service Plan](https://azure.microsoft.com/en-us/pricing/details/app-service) you choose for the web app, your instance is allocated certain amout of RAM. If you data size is much bigger than the published benchmarks **"TODO: Add link when ready"**, web-jobs infrastructure gives up on the task after a certain time. This is due to an [open issue](https://github.com/Azure/azure-webjobs-sdk/issues/899) in azure web job infrastructure.
    
    **Recommendation:** Either move to a higher tiered app service plan from your web app in Azure Portal, or to reduce your usage and catalog size.

6. ##### Only one model gets trained at a time. How can I train more models simultaneously?
    To ensure performance, only one model training is allowed per App Service instance at a given time. However the number of instances can be increased by scaling up the app service. See [Scale instance count manually or automatically](https://docs.microsoft.com/en-us/azure/monitoring-and-diagnostics/insights-how-to-scale?toc=%2fazure%2fapp-service-web%2ftoc.json)

7. ##### Storage exceptions in logs
    As explained in the *High Level Architecture section* of [README](../README.md), the solution uses azure storage queues, tables and blobs for various operations. To handle any networking glitches, the storage SDK client has been configured with retries.
 Blobs and Queues are configured with exponential retry policy while Storage is configured with linear retry policy. More details here - [Azure Storage retry guidelines](https://docs.microsoft.com/en-us/azure/architecture/best-practices/retry-service-specific#azure-storage-retry-guidelines).

     | Parameter | Default value | Description |
     | - | :-: | - |
     | blobClientServerTimeoutMinutes | 20 minutes | Server timeout interval for the request. |
     | blobClientExponentialRetryDeltaBackoffSeconds | 4 seconds | Back-off interval between retries.  |
     | blobClientExponentialRetryMaxAttempts | 5 | Maximum retry attempts. |
     | queueClientExponentialRetryDeltaBackoffSeconds | 4 seconds | Back-off interval between retries. |
     | queueClientExponentialRetryMaxAttempts | 5 | Maximum retry attempts. |
     | tableClientLinearRetryDeltaBackoffSeconds | 0.5 seconds | Back-off interval between retries.  |
     | tableClientLinearRetryMaxAttempts | 5 | Maximum retry attempts. |


   If desired, these values can be over-written by adding the above parameter in *Application Settings* of the *App Service.*
   
![App Settings Configuration](../images/app-settings-configuration.png)

8. ##### Scoring latency is degraded
    There are two possible scenarios where scoring latency could be degraded.
    1. Degradation on first call - When a model is not in cache (see [Architecture](architecture.md)), on a scoring call, it is downloaded from blob storage and loaded in memory. This results in a longer latency (we have seen 0.5-2 seconds), however once this is in the cache, subsequent scoring calls are much faster (<500 msec).
    2. Model training is in progress - Since training happens on the same machine, CPU and memory resources are shared. This results in more processing time of a request. Although increasing the number of instannces by Scaling out can help here, requests going to the machine where training is in progress will still be affected.
    One idea is to [deploy](deployment-instructions.md) two instances  of the service, reconfigure the second service to have the same  *Application Settings*, specifically the two *Connection Strings* - *AzureWebJobsDashboard* and *AzureWebJobsStorage*. Use first deployment exclusively for training and second exclusively for scoring. This would ensure that training doesn't effect scoring.
    ![App Settings Connectionstrings](../images/app-settings-connectionstrings.png)


## Frequently Asked Questions:

##### Q. Where can i see and change my access keys?
    There are two kind of keys that are used - *adminKey* - used for all API operations, *recommendKey* - used only to get recommendation. Both the keys have a primay and secondary key which can be viewed/changed via "Application Settings" of the deployed "App Service" on [Azure Portal](http://portal.azure.com). 

##### Q. How can I cancel a model training which is in progress
    Same as how you delete a model using the "DELETE" operation. See [Api Reference](api-reference.md)

##### Q. Why I cannot select "Free" and "Shared" [App Service Plans](https://azure.microsoft.com/en-us/pricing/details/app-service)?
    The solution can **only** be run on 64-bit machines. Since "Free" and "Shared" plans only provide 32-bit machines it is not possible to use those plans.

##### Q. How can I use a custom domain?
    See [buy domains](https://docs.microsoft.com/en-us/azure/app-service-web/custom-dns-web-site-buydomains-web-app) and [3rd party domains](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-custom-domain-name).

##### Q. How can I configure auto-scaling of my web app to handle more load?
    Auto scale can be configured from azure portal. This [article](https://blogs.msdn.microsoft.com/devschool/2015/05/24/azure-how-to-auto-scale-your-web-apps-web-sites/) provides a good overview on how to set things up.
