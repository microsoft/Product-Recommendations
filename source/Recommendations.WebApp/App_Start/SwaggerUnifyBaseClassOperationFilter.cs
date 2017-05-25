// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Make sure that ModelsRecommend controller and Models controller end up in the same operation family.
    /// </summary>
    public class SwaggerUnifyBaseClassOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Replaces 'ModelsRecommend_' operation id prefix with 'Models_' to unify the controllers
        /// </summary>
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            operation.operationId = operation.operationId.Replace("ModelsRecommend_", "Models_");
        }
    }
}