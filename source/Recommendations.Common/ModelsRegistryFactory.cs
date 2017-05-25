// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using Recommendations.Common.Cloud;
using Recommendations.Core;

namespace Recommendations.Common
{
    /// <summary>
    /// A factory class for creating <see cref="ModelsRegistry"/> instances
    /// </summary>
    public static class ModelsRegistryFactory
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ModelsRegistry"/> that is backed by an Azure storage account table
        /// </summary>
        /// <returns>A new instance of <see cref="ModelsRegistry"/></returns>
        public static ModelsRegistry CreateModelsRegistry()
        {
            // get the storage connection string from configuration
            string storageAccountConnectionString =
                ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;

            // parse the connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            // create a storage tables client
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Linear policy for interactive scenarios
            string tableClientLinearRetryDeltaBackoffSeconds =
                ConfigurationManager.AppSettings["TableClientLinearRetryDeltaBackoffSeconds"];
            double deltaBackoffSeconds;
            if (!double.TryParse(tableClientLinearRetryDeltaBackoffSeconds, out deltaBackoffSeconds))
            {
                deltaBackoffSeconds = 0.5;
            }

            string tableClientLinearRetryMaxAttempts =
                ConfigurationManager.AppSettings["TableClientLinearRetryMaxAttempts"];
            int maxAttempts;
            if (!int.TryParse(tableClientLinearRetryMaxAttempts, out maxAttempts))
            {
                maxAttempts = 5;
            }

            Trace.TraceVerbose(
                $"Setting the table client's retry policy to linear retry with a delta-backoff of {deltaBackoffSeconds} seconds and a maximal attempts count of {maxAttempts}");
            tableClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(deltaBackoffSeconds), maxAttempts);

            // create the models table if not exists
            CloudTable modelsTable = tableClient.GetTableReference(ModelsTableName);
            modelsTable.CreateIfNotExists();

            // create and return a new instance of the model registry
            return new ModelsRegistry(new AzureCloudTable(modelsTable));
        }

        private const string ModelsTableName = "models";
        private static readonly ITracer Trace = new Tracer(nameof(ModelsRegistryFactory));
    }
}