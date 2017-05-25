// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Web.Hosting;
using System.Web.Http;
using Recommendations.WebApp;
using Swashbuckle.Application;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(SwaggerConfig), nameof(SwaggerConfig.Register))]

namespace Recommendations.WebApp
{
    /// <summary>
    /// Swagger configuration file
    /// </summary>
    public class SwaggerConfig
    {
        /// <summary>
        /// Register configuration
        /// </summary>
        public static void Register()
        {
            const string serviceDescription = "The Recommendations API identifies consumption patterns from your transaction information in order to provide recommendations. These recommendations can help your customers more easily discover items that they may be interested in.<br> By showing your customers products that they are more likely to be interested in, you will, in turn, increase your sales.";

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.Schemes(new[] {"https"});
                    c.SingleApiVersion("v1", "Recommendations API")
                        //.Contact(contact => contact
                        //    .Name("")
                        //    .Email("")
                        //    .Url(""))
                        .Description(serviceDescription);

                    // unifying the operation ids prefix at it yields better results when generating AutoRest clients
                    c.OperationFilter<SwaggerUnifyBaseClassOperationFilter>();

                    // add xml comments for public types defined in this project
                    c.IncludeXmlComments(HostingEnvironment.MapPath("/bin/Recommendations.WebApp.XML"));

                    // add xml comments for public types defined in Recommendations.Common
                    // the path of Recommendations.Common.xml is different in a deployed environment 
                    c.IncludeXmlComments(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null
                        ? HostingEnvironment.MapPath("/app_data/jobs/continuous/Recommendations-WebJob/Recommendations.Common.xml")
                        : HostingEnvironment.MapPath("/bin/Recommendations.Common.xml"));

                    c.DescribeAllEnumsAsStrings();

                    // removing cancellation tokens from API method definitions
                    c.OperationFilter<SwaggerRemoveCancellationTokenParameterFilter>();
                    c.DocumentFilter<SwaggerRemoveCancellationTokenDocumentFilter>();

                    // Adds an x-ms-enum extension to each enum type definition in the swagger 
                    c.DocumentFilter<SwaggerAddXmsEnumSchemaToEnumTypesDocumentFilter>();

                    // define a custom swagger document provider for caching the generated document 
                    c.CustomProvider(defaultProvider => new CachingSwaggerProvider(defaultProvider));

                    // define an api key header
                    c.ApiKey(ApiKeyAuthorizationFilterAttribute.ApiKeyHeaderName)
                        .Description("API Key Authentication")
                        .Name(ApiKeyAuthorizationFilterAttribute.ApiKeyHeaderName)
                        .In("header");

                    // group all actions under a single group
                    c.GroupActionsBy(apiDesc => "Operations");
                })
                .EnableSwaggerUi(c =>
                {
                    c.EnableApiKeySupport(ApiKeyAuthorizationFilterAttribute.ApiKeyHeaderName, "header");
                    c.DisableValidator();
                });
        }
    }
}
