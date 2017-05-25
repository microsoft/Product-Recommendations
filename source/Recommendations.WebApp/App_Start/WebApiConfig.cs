// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Net.Http.Formatting;
using System.Web.Http;
using Newtonsoft.Json;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Configuration class
    /// </summary>

    public static class WebApiConfig
    {
        /// <summary>
        /// Method to register http related configuration
        /// </summary>
        /// <param name="config"></param>
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new {id = RouteParameter.Optional});

            // do not serialize null valued properties
            JsonMediaTypeFormatter jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            // remove all media type formatters except JSON
            config.Formatters.Clear();
            config.Formatters.Add(jsonFormatter);

            // add the exception handler
            config.MessageHandlers.Add(new ExceptionHandler());
        }
    }
}
