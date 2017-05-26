## Azure App Service

The solution will run as an [Azure WebApp](https://azure.microsoft.com/en-us/services/app-service/web/).
Here you need to select the application [hosting plan](https://azure.microsoft.com/en-us/pricing/details/app-service/). 
Note that **you can always change** the plan (scale up) or increase the number of instances (scale out) from Azure Portal even after you've deployed the service.
>**Important**: The selected plan will determine the size of your machine, and therefore the number of models you can concurrently train and 
> the latency of get-recommendation requests. You can [use these benchmarks](https://go.microsoft.com/fwlink/?linkid=850656) to help you choose the right one to start with 
>and later on adjust the plan according to your specific needs and observed latencies.
