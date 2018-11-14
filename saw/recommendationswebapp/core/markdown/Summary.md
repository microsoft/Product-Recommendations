# Recommendations Solution

This solution enables you to create product recommendations predictive models based on historical transaction data and information on the product catalog.
At a high level, the solution exposes mechanisms to:
1. Train models using the SAR (Smart Adaptive Recommendations) algorithm.
2. Request a previously created model for recommendations.

The following scenarios are supported by the SAR algorithm:

**Item-to-Item Recommendations.** This is the "Customers who liked this product also liked these other products" scenario. Increase the discoverability of items in your catalog by showing relevant products to your customers.

**Personalized Recommendations.** By providing a user id or the recent history of transactions for a given user, the SAR algorithm can return personalized recommendations for that user.

## High level architecture

This solution creates the Azure resources necessary and connects them to generate a scalable architecture.  More specifically, it creates an Azure Resource Group in your Azure subscription with the following components:

1. An Azure WebApp (and a respective Web Job) The Azure Web-Application exposes a RESTful interface that allows you to train recommendations models, and then query those models for product recommendations. The Azure Web-Application also delegates training jobs to an Azure WebJob.
2. An Azure Storage subscription that is used for storing models, model metadata as well as for WebApp to WebJob communication.


![Diagram](/saw/recommendationswebapp/assets/highlevelarch.png)

Learn more about the Recommendations Solution [here](http://github.com/Microsoft/Product-Recommendations).
