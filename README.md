<img  src="images/logo.png" align="right" height="200px">

# Product Recommendations Preconfigured Solution

## Overview

This solution enables you to create product recommendations predictive models based on historical transaction data and information on the product catalog.

At a high level, The solution exposes mechanisms to:
1. Build models using a SAR (Smart Adaptive Recommendations) algorithm. 
2. Request a previously created model for recommendations.

The following scenarios are supported by a SAR algorithm:

1. Item-to-Item recommendations.

This is the "Customers who liked this product also liked these other products" scenario.
Increase the discoverability of items in your catalog by showing relevant products to your customers.

2. User-to-Item recommendations.

By providing the recent transactions for a given user, the SAR algorithm can return personalized recommendations for that user. 


## Deployment Instructions

Read the [Deployment Instructions](deployment-instructions.md) document that explains the steps to follow to deploy the solution to your own Azure subscription. 

## Training your first model

Once you have deployed the solution, check out the [API Reference](api-reference.md).

Then you will be ready to follow step-by-step instructions on how to create your first model
using the [Getting Started Guide](getting-started.md).


## High level architecture

This solution creates a new Azure Resource Group to your Azure subscription with the following components:

1. An [Azure WebApp](https://azure.microsoft.com/en-us/services/app-service/web/) (and respective web jobs)
The Azure Web-Application exposes a RESTful interface (See API Reference section) that allows you to train
recommendations models, and then query those models for product recommendations. The Azure Web-Application also
delegates training jobs  to an [Azure WebJob](https://docs.microsoft.com/en-us/azure/app-service-web/websites-webjobs-resources).

2. An [Azure Storage](https://azure.microsoft.com/en-us/services/storage) subscription that is used for storing models, 
model metadata as well as messages between the WebApp and the WebJob.

![Architecture Diagram](images/architecture-diagram.png)

## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
