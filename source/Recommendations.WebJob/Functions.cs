// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Recommendations.Common;
using Recommendations.Common.Api;
using Recommendations.Common.Cloud;
using Recommendations.Core;

namespace Recommendations.WebJob
{
    public class Functions
    {
        /// <summary>
        /// Handles messages from the train model queue
        /// </summary>
        public static async Task ProcessTrainModelQueueMessage(
            [QueueTrigger(AzureModelQueueFactory.TrainModelQueueName)] ModelQueueMessage message,
            int dequeueCount,
            CancellationToken cancellationToken)
        {
            // set the model id to context
            Guid modelId = message.ModelId;
            ContextManager.ModelId = modelId;
            
            // Note: The following if statement technically should never resolve to true as the web job infra should 
            // handle max dequeued counts, However, due to issue https://github.com/Azure/azure-webjobs-sdk/issues/899, 
            // if the job takes too long to finish, infrastructure marks it as "never finished" and doesn't move it to 
            // the poison queue even if the failure threshold is met. This results in a infinite loop for the message.
            // Here we manually update the status and return successful to avoid the loop.
            if (dequeueCount > MaxDequeueCount)
            {
                Trace.TraceError($"Aborting model training after {dequeueCount - 1} attempts");
                await MarkModelAsFailed(modelId, cancellationToken);
                return;
            }

            Trace.TraceInformation("Start handling train model queue message");
            
            // create a logic class using the model registry and provider
            var logic = new WebJobLogic(ModelsProvider.Value, ModelsRegistry.Value);

            try
            {
                // train the model
                await logic.TrainModelAsync(modelId, cancellationToken);
            }
            catch (TaskCanceledException exception)
            {
                // the training was cancelled
                Trace.TraceInformation($"Training of model was cancelled. Exception: '{exception}'");

                // check if the cancelation was external
                if (cancellationToken.IsCancellationRequested)
                {
                    // add model to delete queue
                    Trace.TraceInformation("Queueing a message to delete model resources");
                    await DeleteModelQueue.Value.AddMessageAsync(
                        new ModelQueueMessage {ModelId = modelId}, CancellationToken.None);
                }

                // throw the cancellation exception so that the Web job infrastructure could handle
                throw;
            }
            catch (Exception exception)
            {
                string errorMessage = $"Training #{dequeueCount} out of {MaxDequeueCount} failed with exception: '{exception}'";
                Trace.TraceWarning(errorMessage);

                Trace.TraceInformation($"Updating model '{modelId}' status message with the error message: '{errorMessage}'");
                await ModelsRegistry.Value.UpdateModelAsync(modelId, cancellationToken, statusMessage: errorMessage);

                // throw the exception and rely on the Web job infrastructure for retries
                throw;
            }

            Trace.TraceInformation("Successfully finished handling train model queue message");
        }

        /// <summary>
        /// Handles messages from the train model poison queue
        /// </summary>
        public static async Task ProcessTrainModelPoisonQueueMessage(
            [QueueTrigger(AzureModelQueueFactory.TrainModelQueueName + PoisonQueueSuffix)] ModelQueueMessage message,
            CancellationToken cancellationToken)
        {
            // set the model id to context
            Guid modelId = message.ModelId;
            ContextManager.ModelId = modelId;

            Trace.TraceInformation($"Handling model training poison message for model {modelId}");

            // mark model as failed
            await MarkModelAsFailed(modelId, cancellationToken);
            Trace.TraceVerbose("Finished handling poison train model message");
        }
        
        /// <summary>
        /// Handles messages from the delete model queue
        /// </summary>
        public static async Task ProcessDeleteModelQueueMessage(
            [QueueTrigger(AzureModelQueueFactory.DeleteModelQueueName)] ModelQueueMessage message,
            CancellationToken cancellationToken)
        {
            // set the model id to context
            Guid modelId = message.ModelId;
            ContextManager.ModelId = modelId;
            
            Trace.TraceInformation($"Start handling the deletion of model {modelId}");
            
            try
            {
                Trace.TraceVerbose("Deleting model from model provider");
                await ModelsProvider.Value.DeleteModelAsync(modelId, cancellationToken);
            }
            catch (Exception exception)
            {
                // throw the exception and rely on the Web job infrastructure for retries
                Trace.TraceWarning($"Failed deleting model using the model provider. Exception: '{exception}'");
                throw;
            }

            Trace.TraceInformation($"Successfully completed handling of model '{modelId}' deletion message");
        }

        /// <summary>
        /// Handles messages from the delete model poison queue
        /// </summary>
        public static void ProcessDeleteModelPoisonQueueMessage(
            [QueueTrigger(AzureModelQueueFactory.DeleteModelQueueName + PoisonQueueSuffix)] ModelQueueMessage message)
        {
            Trace.TraceWarning($"Failed handling delete model message for model '{message?.ModelId}'");
        }

        /// <summary>
        /// Marks a model as failed and deletes the model's user history table if exists
        /// </summary>
        private static async Task MarkModelAsFailed(Guid modelId, CancellationToken cancellationToken)
        {
            try
            {
                Trace.TraceVerbose("Deleting model from model provider");
                await ModelsProvider.Value.DeleteModelAsync(modelId, cancellationToken);

                Trace.TraceInformation($"Marking model with id '{modelId}' as {ModelStatus.Failed}");
                await ModelsRegistry.Value.UpdateModelAsync(modelId, cancellationToken, ModelStatus.Failed);
            }
            catch (Exception exception)
            {
                Trace.TraceWarning($"Failed updating model status to '{ModelStatus.Failed}'. Exception: '{exception}'");
            }
        }

        private static readonly Lazy<ModelsRegistry> ModelsRegistry =
            new Lazy<ModelsRegistry>(ModelsRegistryFactory.CreateModelsRegistry);

        private static readonly Lazy<ModelsProvider> ModelsProvider =
            new Lazy<ModelsProvider>(ModelsProviderFactory.CreateModelsProvider);

        private static readonly Lazy<IModelQueue> DeleteModelQueue =
            new Lazy<IModelQueue>(AzureModelQueueFactory.CreateDeleteModelQueue);
        
        private const string PoisonQueueSuffix = "-poison";
        private const int MaxDequeueCount = 2;
        private static readonly ITracer Trace = new Tracer(nameof(Functions));
    }
}
