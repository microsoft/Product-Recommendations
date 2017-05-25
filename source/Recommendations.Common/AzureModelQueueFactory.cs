// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Recommendations.Common.Cloud;

namespace Recommendations.Common
{
    /// <summary>
    /// A factory class for creating <see cref="IModelQueue"/> instances
    /// </summary>
    public static class AzureModelQueueFactory
    {
        /// <summary>
        /// Creates a queue for model training messages
        /// </summary>
        /// <returns>The created queue</returns>
        public static IModelQueue CreateTrainModelQueue()
        {
            return CreateModelQueue(TrainModelQueueName);
        }

        /// <summary>
        /// Creates a queue for delete model messages
        /// </summary>
        /// <returns>The created queue</returns>
        public static IModelQueue CreateDeleteModelQueue()
        {
            return CreateModelQueue(DeleteModelQueueName);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AzureModelQueue"/> class.
        /// </summary>
        /// <param name="queueName">The name of the queue to create</param>
        private static AzureModelQueue CreateModelQueue(string queueName)
        {
            // get the storage connection string from configuration
            string storageAccountConnectionString =
                ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString;

            // parse the connection string
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            // create a queue client
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Queues are used for batch/background operations which are non-interactive. So using the ExponentialRetry 
            // policy to allow more time for the service to recover—with a consequently increased chance of the operation 
            // eventually succeeding.
            double deltaBackoffSeconds;
            if (!double.TryParse(ConfigurationManager.AppSettings["QueueClientExponentialRetryDeltaBackoffSeconds"],
                out deltaBackoffSeconds))
            {
                deltaBackoffSeconds = 4;
            }

            int maxAttempts;
            if (!int.TryParse(ConfigurationManager.AppSettings["QueueClientExponentialRetryMaxAttempts"],
                out maxAttempts))
            {
                maxAttempts = 5;
            }

            // set the retry policy
            queueClient.DefaultRequestOptions.RetryPolicy =
                new ExponentialRetry(TimeSpan.FromSeconds(deltaBackoffSeconds), maxAttempts);

            // get a reference to the command queue
            CloudQueue modelingQueueClient = queueClient.GetQueueReference(queueName);

            // create the queue if not exists
            modelingQueueClient.CreateIfNotExists();

            // create the command queue
            return new AzureModelQueue(modelingQueueClient);
        }

        /// <summary>
        /// Train model queue name
        /// </summary>
        public const string TrainModelQueueName = "commandqueue";
        
        /// <summary>
        /// Delete model queue name
        /// </summary>
        public const string DeleteModelQueueName = "deletequeue";
    }
}