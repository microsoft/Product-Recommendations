// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Recommendations.Core;

namespace Recommendations.Common
{
    /// <summary>
    /// An interface for a providing user history document store instances.
    /// </summary>
    public interface IDocumentStoreProvider
    {
        /// <summary>
        /// Gets a document store associated with a model
        /// </summary>
        /// <param name="modelId">The model id in context</param>
        /// <returns>A handle to the model's document store</returns>
        IDocumentStore GetDocumentStore(Guid modelId);
    }
}