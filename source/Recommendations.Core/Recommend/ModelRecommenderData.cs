// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.MachineLearning.Recommend;

namespace Recommendations.Core.Recommend
{
    public class ModelRecommenderData
    {
        /// <summary>
        /// Gets the underlying <see cref="IUserHistoryToItemsRecommender"/>
        /// </summary>
        internal IUserHistoryToItemsRecommender Recommender { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelRecommenderData"/> class.
        /// </summary>
        /// <param name="recommender">the underlying SAR recommender</param>
        internal ModelRecommenderData(IUserHistoryToItemsRecommender recommender)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            Recommender = recommender;
        }
    }
}