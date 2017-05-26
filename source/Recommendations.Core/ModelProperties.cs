// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace Recommendations.Core
{
    [Serializable]
    public class ModelProperties
    {
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
        /// Indicates whether user to item recommendations is supported or not.
        /// </summary>
        public bool IsUserToItemRecommendationsSupported { get; set; }

        /// <summary>
        /// Gets or sets reference date to consider while scoring
        /// </summary>
        public DateTime? ReferenceDate { get; set; }

        /// <summary>
        /// Gets or sets The decay period to consider while scoring
        /// </summary>
        public TimeSpan? Decay { get; set; }
        
        /// <summary>
        /// Gets or sets the number of unique users found in the usage files used for training the model
        /// </summary>
        public int UniqueUsersCount { get; set; }
    }
}