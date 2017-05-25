// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace Recommendations.WebApp.Models
{
    /// <summary>
    /// Represent the result of a get recommendation operation
    /// </summary>
    public class RecommendationResult
    {
        /// <summary>
        /// The recommended item id
        /// </summary>
        [JsonProperty("recommendedItemId")]
        public string RecommendedItemId { get; set; }

        /// <summary>
        /// The score of this recommendation
        /// </summary>
        [JsonProperty("score")]
        public double Score { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="RecommendationResult"/> class.
        /// </summary>
        /// <param name="itemId">The recommended item id</param>
        /// <param name="score">The score of this recommendation</param>
        public RecommendationResult(string itemId, double score)
        {
            RecommendedItemId = itemId;
            Score = score;
        }
    }
}