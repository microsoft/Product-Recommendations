// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace Recommendations.Core.Recommend
{
    public class RecommenderSettings
    {
        /// <summary>
        /// Gets or sets the trained model local file path
        /// </summary>
        public string TrainedModelFilePath { get; set; }

        /// <summary>
        /// Gets or sets the item id index local file path
        /// </summary>
        public string ItemIdIndexFilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include input items in the recommendation
        /// </summary>
        public bool IncludeHistory { get; set; }

        /// <summary>
        /// For user-to-item recommendations, it defines whether the event timestamp and weight 
        /// should be considered when scoring.
        /// </summary>
        public bool EnableUserAffinity { get; set; }

        /// <summary>
        /// Gets or sets reference date to consider while scoring
        /// </summary>
        public DateTime? ReferenceDate { get; set; }

        /// <summary>
        /// Gets or sets The decay period to consider while scoring
        /// </summary>
        public TimeSpan? Decay { get; set; }
    }
}
