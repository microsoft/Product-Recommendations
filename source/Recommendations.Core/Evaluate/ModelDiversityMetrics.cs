// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Recommendations.Core.Evaluate
{
    /// <summary>
    /// Diversity metrics for a model. Diversity gives customers a sense of how diverse the item recommendations are,
    /// based on their usage shown by bucket eg: 0-90, 90-99, 99-100. In simple terms, how many recommendations are 
    /// coming from most popular items, how many from non-popular etc., unique items recommended.
    /// </summary>
    public class ModelDiversityMetrics
    {
        /// <summary>
        /// PercentileBucket representing a bucket for the metric
        /// </summary>
        [JsonProperty("percentileBuckets")]
        public IList<PercentileBucket> PercentileBuckets { get; set; }

        /// <summary>
        /// Total number of items recommended. (some may be duplicates)
        /// </summary>
        [JsonProperty("totalItemsRecommended")]
        public int? TotalItemsRecommended { get; set; }

        /// <summary>
        /// Total number of distinct items that were returned for evaluation.
        /// </summary>
        [JsonProperty("uniqueItemsRecommended")]
        public int? UniqueItemsRecommended { get; set; }

        /// <summary>
        /// Total number of distinct items in the train dataset.
        /// </summary>
        [JsonProperty("uniqueItemsInTrainSet")]
        public int? UniqueItemsInTrainSet { get; set; }
    }
}