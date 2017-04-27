## Frequently Asked Questions:

##### Q. Where can i see and change my access keys?
>There are two kind of keys that are used - *adminKey* - used for all API operations, *recommendKey* - used only to get recommendation. Both the keys have a primay and secondary key which can be viewed/changed via "Application Settings" of the deployed "App Service" on [Azure Portal](http://portal.azure.com). 

##### Q. I get 415 Unsupported Media type on Post requests
>Ensure the header - "Content-Type: application/json" is set.

##### Q. How can I cancel a model training which is in progress
>Same as how you delete a model using the "DELETE" operation. See [Api Reference](doc/api-reference.md)

##### Q. How can I use a custom domain?
>See [buy domains](https://docs.microsoft.com/en-us/azure/app-service-web/custom-dns-web-site-buydomains-web-app) and [3rd party domains](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-custom-domain-name).



