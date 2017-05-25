// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace Recommendations.Core.Evaluate
{
    /// <summary>
    /// Represents diversity metric for a specific popularity bucket.
    /// </summary>
    public class PercentileBucket
    {
        /// <summary>
        /// The beginning percentile of the popularity bucket (inclusive).
        /// </summary>
        [JsonProperty("min")]
        public int Min { get; set; }

        /// <summary>
        /// The ending percentile of the popularity bucket (exclusive).
        /// </summary>
        [JsonProperty("max")]
        public int Max { get; set; }

        /// <summary>
        /// The fraction of all recommended users that belong to the specified popularity bucket.
        /// </summary>
        [JsonProperty("percentage")]
        public double Percentage { get; set; }
    }
}