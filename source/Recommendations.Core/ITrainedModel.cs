// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Recommendations.Core.Recommend;

namespace Recommendations.Core
{
    /// <summary>
    /// Represents a trained model
    /// </summary>
    public interface ITrainedModel
    {
        /// <summary>
        /// Gets the trained model properties
        /// </summary>
        ModelProperties Properties { get; }

        /// <summary>
        /// Gets the indexed item ids 
        /// </summary>
        string[] ItemIdIndex { get; }

        /// <summary>
        /// Gets the model recommender data
        /// </summary>
        ModelRecommenderData RecommenderData { get; }
    }
}