// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Adds an x-ms-enum extension to each enum type definition in the swagger 
    /// See: https://github.com/Azure/azure-rest-api-specs/blob/master/documentation/creating-swagger.md#Enum-x-ms-enum
    /// </summary>
    internal class SwaggerAddXmsEnumSchemaToEnumTypesDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            // extract all the enum typed properties
            IEnumerable<KeyValuePair<string, Schema>> enumPropertiesSchemas =
                swaggerDoc.definitions?.Values.SelectMany(definition => definition.properties)
                    .Where(property => property.Value.@enum != null)
                ?? Enumerable.Empty<KeyValuePair<string, Schema>>();

            // add the extension to the found definitions
            foreach (var enumProperty in enumPropertiesSchemas)
            {
                string enumTypeName = char.ToUpper(enumProperty.Key[0]) + enumProperty.Key.Substring(1);
                enumProperty.Value.vendorExtensions.Add("x-ms-enum", new {name = enumTypeName, modelAsString = false});
            }
        }
    }
}