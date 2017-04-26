![Debugging](images/debugging.gif)

---

### Enabling Logging

To make the debugging process easier, necessary telemetry has been added throughout the source code. By default, diagnostics is disabled in azure web apps and can be enabled by following the steps here - [Enable diagnostics logging for web apps in Azure App Service](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-enable-diagnostic-log).

**Tip:** Stream logs and console output directly from azure portal. More details are explained here - [Streaming Logs and the Console](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-streaming-logs-and-console).

---

### Common Scenarios

1. ##### API call fails with 5xx status codes
>When using _admin_ key, a detailed exception message along with inner exceptions are returned to help understand the potential problem. Note that this stack is not returned with the _recommend_ key.


2. ##### Model training request fails with 400 status codes.
> In most cases, a clear error message indicating the issue is returned. 
 ![400Withmessage](images/400withmessage.png)
 In some cases, error message might not point to exact point of error. This happens mainly if the body is not properly composed. Eg: a boolean parameter has a value other than true/false.
 
 >![400Withoutmessage](images/400withoutmessage.png)

3. ##### Model takes forever to train. Eventually fails with status "aborted after n attempts"
>The recommendations algorithm is a memory based algorithm. Based on the [App Service Plan](https://azure.microsoft.com/en-us/pricing/details/app-service) you choose for the web app, your instance is allocated certain amout of RAM. If you data size is much bigger than the published benchmarks **"TODO: Add link when ready"**, web-jobs infrastructure gives up on the task after a certain time. This is due to an [open issue](https://github.com/Azure/azure-webjobs-sdk/issues/899) in azure web job infrastructure.
>
>**Recommendation:** Either move to a higher tiered app service plan from your web app in Azure Portal, or to reduce your usage and catalog size.

4. ##### Only one model gets trained at a time. How can I train more models simultaneously?
>To ensure performance, only one model training is allowed per App Service instance at a given time. However the number of instances can be increased by scaling up the app service. See [Scale instance count manually or automatically](https://docs.microsoft.com/en-us/azure/monitoring-and-diagnostics/insights-how-to-scale?toc=%2fazure%2fapp-service-web%2ftoc.json)

5. ##### API call fails with 401 Unauthorized
>1. Ensure you are passing the api key header - "x-api-key:<*key*>".
>2. For all non recommendation APIs, ensure you are using *adminKey*.

6. ##### Storage exceptions in logs
>As explained in the *High Level Architecture section* of [README](README.MD), the solution uses azure storage queues, tables and blobs for various operations. To handle any networking glitches, the storage sdk client has been configured with retries.
 Blobs and Queues are configured with exponential retry policy while Storage is configured with linear retry policy. More details here - [Azure Storage retry guidelines](https://docs.microsoft.com/en-us/azure/architecture/best-practices/retry-service-specific#azure-storage-retry-guidelines).
   
>| Parameter | Default value | Description |
 | - | :-: | - |
 | blobClientServerTimeoutMinutes | 20 minutes | Server timeout interval for the request. |
 | blobClientExponentialRetryDeltaBackoffSeconds | 4 seconds | Back-off interval between retries.  |
 | blobClientExponentialRetryMaxAttempts | 5 | Maximum retry attempts. |
 | queueClientExponentialRetryDeltaBackoffSeconds | 4 seconds | Back-off interval between retries. |
 | queueClientExponentialRetryMaxAttempts | 5 | Maximum retry attempts. |
 | tableClientLinearRetryDeltaBackoffSeconds | 0.5 seconds | Back-off interval between retries.  |
 | tableClientLinearRetryMaxAttempts | 5 | Maximum retry attempts. |

   >If desired, these values can be over-written by adding the above parameter in *Application Settings* of the *App Service.*
![App Settings Configuration](images/app-settings-configuration.png)

7. ##### Scoring latency is degraded
> There are two possible scenarios where scoring latency could be degraded.
> 1. Model training is in progress - Since training happens on the same machine, cpu and memory resources are shared thus resulting in more processing time of a request.
> 2. Degradation on first call - When a model is not in cache (see [Architecture](architecture.md)), on a scoring call, it is downloaded from blob storage and loaded in memory. This results in a longer latency (we have seen 0.5-2 seconds), however once this is in the cache, subsequent scoring calls are much faster (<500 msec).
