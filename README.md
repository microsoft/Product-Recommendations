# Product Recommendations Preconfigured Solution


## Overview

This solution enables a product recommendations predictive model. It uses previous customer transactions
and information on the product catalog to train models that can later on be queried for product recommendations.

At a high level, The solution exposes a mechanisms to:
1. Build models using a SAR (Smart Adaptive Recommendations) algorithm. 
2. Request a previously created model for recommendations.

The following scenarios are supported by a SAR algorithm:

1. Item-to-Item recommendations.

This is the "Customers who liked this product also liked these other products" scenario.
Increase the discoverability of items in your catalog by showing relevant products to your customers.

2. User-to-Item recommendations.

By providing the recent transactions for a given user, the SAR algorithm can return personalize recommendations for that user. 


## Deployment Instructions

The preconfigured solution can be easily set up following a Wizard.


The [Deployment Instructions](deployment-instructions.md) explain the set of steps 
to follow to deploy the solution to your Azure subscription.

## Training your first model

Once you have deployed the solution, you may want to check out the [API Reference](api-reference.md).


You may also want to follow step-by-step instruction on how to create your first model
using the [Getting Started Guide](getting-started.md)


## High level architecture

This solution creates a new Azure Resource Group to your Azure subscription with the following components:

1. An [Azure WebApp](https://azure.microsoft.com/en-us/services/app-service/web/) (and respective web jobs)
The Azure Web-Application exposes a RESTful interface (See API Reference section) that allows you to train
recommendations models, and then query those models for product recommendations. The Azure Web-Application also
delegates training jobs  to an [Azure WebJob](https://docs.microsoft.com/en-us/azure/app-service-web/websites-webjobs-resources).

2. An [Azure Storage](https://azure.microsoft.com/en-us/services/storage) subscription that is used for storing models, 
model metadata as well as messages between the WebApp and the WebJob.

3. An app service plan
  // TODO : Do we need to say this?

TODO TODO

Insert a high level architecture diagram here.

TODO TODO

## Getting Started Guide
Check out the getting started guide (TODO Insert link here). It explain how to deploy the service, and train your
first model.






## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
