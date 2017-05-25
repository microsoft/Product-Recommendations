// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Core.Recommend
{
    public class Recommendation
    {
        /// <summary>
        /// Gets or set the recommended item Id
        /// </summary>
        public string RecommendedItemId { get; set; }

        /// <summary>
        /// Gets or sets the recommendation's score
        /// </summary>
        public double Score { get; set; }
    }
}