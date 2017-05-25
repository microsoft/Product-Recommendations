// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Recommendations.Core;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// An implementation of <see cref="IModelQueue"/> using an underlying Azure storage queue
    /// </summary>
    public class AzureModelQueue : IModelQueue
    {
        /// <summary>
        /// creates a new instance of the <see cref="AzureModelQueue"/> class.
        /// </summary>
        /// <param name="queue">The underlying queue to use</param>
        public AzureModelQueue(CloudQueue queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            _cloudQueue = queue;
        }

        /// <summary>
        /// Add a <see cref="ModelQueueMessage"/> message to the queue
        /// </summary>
        /// <param name="message">The message to add</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        public async Task AddMessageAsync(ModelQueueMessage message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Trace.TraceVerbose($"Adding message with model id '{message.ModelId}' to the {_cloudQueue.Name} queue");

            // serialize the input message
            string content = JsonConvert.SerializeObject(message);
            
            // add the message to the queue
            await _cloudQueue.AddMessageAsync(new CloudQueueMessage(content), cancellationToken);
        }

        private readonly CloudQueue _cloudQueue;
        private static readonly ITracer Trace = new Tracer(nameof(AzureModelQueue));
    }
}