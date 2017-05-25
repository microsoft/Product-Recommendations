// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace Recommendations.Core.Evaluate
{
    /// <summary>
    /// Represents precision for a particular value of K.
    /// </summary>
    public class PrecisionMetric
    {
        /// <summary>
        /// The value K used to calculate the metric values
        /// </summary>
        [JsonProperty("k")]
        public int K { get; set; }

        /// <summary>
        /// Precision@K percentage
        /// </summary>
        [JsonProperty("percentage")]
        public double Percentage { get; set; }

        /// <summary>
        /// The total number of users in the test dataset.
        /// </summary>
        [JsonProperty("usersInTest")]
        public int? UsersInTest { get; set; }
    }
}