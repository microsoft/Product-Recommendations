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
    /// An implementation for the <see cref="IDocumentStoreProvider"/> interface using <see cref="AzureDocumentStore"/> instances
    /// </summary>
    public class DocumentStoreProvider : IDocumentStoreProvider
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DocumentStoreProvider"/> .
        /// </summary>
        public DocumentStoreProvider()
        {
            // get the storage connection string from configuration
            string storageAccountConnectionString =
                ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;

            // parse the connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            // create a storage tables client
            _tableClient = storageAccount.CreateCloudTableClient();

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
            _tableClient.DefaultRequestOptions.RetryPolicy =
                new LinearRetry(TimeSpan.FromSeconds(deltaBackoffSeconds), maxAttempts);
        }

        /// <summary>
        /// Gets a document store associated with a model
        /// </summary>
        /// <param name="modelId">The model id in context</param>
        /// <returns>A handle to the model's document store</returns>
        public IDocumentStore GetDocumentStore(Guid modelId)
        {
            string userHistoryTableName = $"userHistory{modelId:N}";
            CloudTable userHistoryTable = _tableClient.GetTableReference(userHistoryTableName);

            Trace.TraceVerbose($"Getting user history document store from model {modelId}");
            return new AzureDocumentStore(userHistoryTable);
        }

        private readonly CloudTableClient _tableClient;
        private static readonly ITracer Trace = new Tracer(nameof(DocumentStoreProvider));
    }
}
