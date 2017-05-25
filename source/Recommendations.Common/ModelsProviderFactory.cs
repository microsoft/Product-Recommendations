// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Configuration;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Recommendations.Common.Cloud;
using Recommendations.Core;

namespace Recommendations.Common
{
    /// <summary>
    /// A factory class for creating <see cref="ModelsProvider"/> instances
    /// </summary>
    public static class ModelsProviderFactory
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ModelsProvider"/> class.
        /// </summary>
        /// <returns>A new instance of <see cref="ModelsProvider"/></returns>
        public static ModelsProvider CreateModelsProvider()
        {
            // get the storage connection string from configuration
            string storageAccountConnectionString =
                ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;

            // parse the connection string
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            // create a blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            string blobClientServerTimeoutMinutes = ConfigurationManager.AppSettings["BlobClientServerTimeoutMinutes"];
            double serverTimeoutMinutes;
            if (!double.TryParse(blobClientServerTimeoutMinutes, out serverTimeoutMinutes))
            {
                serverTimeoutMinutes = 20;
            }

            Tracer.TraceVerbose($"Setting the blob client's server timeout to {serverTimeoutMinutes} minutes");
            blobClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromMinutes(serverTimeoutMinutes);

            // Blobs are used for batch/background operations which are non-interactive. So using the ExponentialRetry 
            // policy to allow more time for the service to recover—with a consequently increased chance of the operation 
            // eventually succeeding.
            string blobClientExponentialRetryDeltaBackoffSeconds =
                ConfigurationManager.AppSettings["BlobClientExponentialRetryDeltaBackoffSeconds"];
            double deltaBackoffSeconds;
            if (!double.TryParse(blobClientExponentialRetryDeltaBackoffSeconds, out deltaBackoffSeconds))
            {
                deltaBackoffSeconds = 4;
            }

            string blobClientExponentialRetryMaxAttempts =
                ConfigurationManager.AppSettings["BlobClientExponentialRetryMaxAttempts"];
            int maxAttempts;
            if (!int.TryParse(blobClientExponentialRetryMaxAttempts,out maxAttempts))
            {
                maxAttempts = 5;
            }

            Tracer.TraceVerbose(
                $"Setting the blob client's retry policy to exponential retry with a delta-backoff of {deltaBackoffSeconds} seconds and a maximal attempts count of {maxAttempts}");
            blobClient.DefaultRequestOptions.RetryPolicy =
                new ExponentialRetry(TimeSpan.FromSeconds(deltaBackoffSeconds), maxAttempts);
            
            // get the local root folder path
            string localRootPath = GetLocalRootFolder();

            // create a local folder for trained model files
            string trainedModelsLocalRootPath = Path.Combine(localRootPath, "models");
            Directory.CreateDirectory(trainedModelsLocalRootPath);

            // create a new document store provider
            IDocumentStoreProvider documentStoreProvider = new DocumentStoreProvider();

            // create a model provider
            return new ModelsProvider(new AzureBlobContainerProvider(blobClient), documentStoreProvider, trainedModelsLocalRootPath);
        }

        /// <summary>
        /// Gets a local file folder to be used as the root folder for writing files
        /// </summary>
        private static string GetLocalRootFolder()
        {
            // check if running in a context of Azure web app 
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
            {
                return Environment.GetEnvironmentVariable("HOME");
            }

            // running in a local environment
            return Environment.GetEnvironmentVariable("TEMP");
        }

        private static readonly ITracer Tracer = new Tracer(nameof(ModelsProviderFactory));
    }
}