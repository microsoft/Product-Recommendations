// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Configuration;
using System.Web.Http;
using Microsoft.ApplicationInsights.Extensibility;
using Recommendations.Common;
using Recommendations.Core;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Web application
    /// </summary>
    public class WebApiApplication : System.Web.HttpApplication
    {
        /// <summary>
        /// Configures application
        /// </summary>
        protected void Application_Start()
        {
            ContextManager.RoleName = "WebApp";

            GlobalConfiguration.Configure(WebApiConfig.Register);

            Tracer.TraceInformation("Create a model training queue and set it to context");
            WebAppContext.TrainModelQueue = AzureModelQueueFactory.CreateTrainModelQueue();

            Tracer.TraceInformation("Create a delete queue and set it to context");
            WebAppContext.DeleteModelQueue = AzureModelQueueFactory.CreateDeleteModelQueue();

            Tracer.TraceInformation("Create a model provider and set it to context");
            WebAppContext.ModelsProvider = ModelsProviderFactory.CreateModelsProvider();

            Tracer.TraceInformation("Creating a model registry and set it to context");
            WebAppContext.ModelsRegistry = ModelsRegistryFactory.CreateModelsRegistry();

            // if Application Insights instrumentation key is provided, setup and enable telemetry 
            string instrumentationKey = ConfigurationManager.AppSettings["ApplicationInsightsInstrumentationKey"];
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                Tracer.TraceInformation("Enabling Application Insights telemetry and trace collection");
                TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;
                TelemetryConfiguration.Active.DisableTelemetry = false;
            }
            else
            {
                Tracer.TraceInformation("Disabling Application Insights telemetry and trace collection");
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }
        }

        private static readonly ITracer Tracer = new Tracer(nameof(WebApiApplication));
    }
}
