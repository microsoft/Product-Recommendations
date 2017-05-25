// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace Recommendations.Core.Sar
{
    internal class SarScoringArguments
    {
        /// <summary>
        /// Gets or sets the number of recommendations to return
        /// </summary>
        public int RecommendationCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include input items in the recommendation
        /// </summary>
        public bool IncludeHistory { get; set; }

        /// <summary>
        /// Gets or sets reference date to consider while scoring
        /// 
        /// </summary>
        public DateTime? ReferenceDate { get; set; }

        /// <summary>
        /// Gets or sets the decay period to use while scoring
        /// </summary>
        public TimeSpan? Decay { get; set; }
    }
}