// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Newtonsoft.Json;
using Recommendations.Core.Evaluate;

namespace Recommendations.Common.Api
{
    /// <summary>
    /// A model evaluation results
    /// </summary>
    public class ModelEvaluationResult
    {
        /// <summary>
        /// The model evaluation duration
        /// </summary>
        [JsonProperty("duration")]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The usage events files parsing report
        /// </summary>
        [JsonProperty("usageEventsParsing")]
        public ParsingReport EvaluationUsageEventsParsingReport { get; set; }

        /// <summary>
        /// The model precision and diversity metrics
        /// </summary>
        [JsonProperty("metrics")]
        public ModelMetrics Metrics { get; set; }
    }
}