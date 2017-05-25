// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// Representing a logical queue of model related messages 
    /// </summary>
    public interface IModelQueue
    {
        /// <summary>
        /// Add a <see cref="ModelQueueMessage"/> message to the queue
        /// </summary>
        /// <param name="message">The message to add</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        Task AddMessageAsync(ModelQueueMessage message, CancellationToken cancellationToken);
    }
}