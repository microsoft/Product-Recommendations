// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Recommendations.Common.Api
{
    /// <summary>
    /// The model statistics gathered during model training
    /// </summary>
    public class ModelStatistics
    {
        /// <summary>
        /// The total duration
        /// </summary>
        [JsonProperty("totalDuration")]
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// The core training duration
        /// </summary>
        [JsonProperty("trainingDuration")]
        public TimeSpan TrainingDuration { get; set; }

        /// <summary>
        /// The duration of storing usage events per user later to be used for user recommendations
        /// </summary>
        [JsonProperty("storingUserHistoryDuration")]
        public TimeSpan? StoringUserHistoryDuration { get; set; }

        /// <summary>
        /// The catalog file parsing report
        /// </summary>
        [JsonProperty("catalogParsing")]
        public ParsingReport CatalogParsingReport { get; set; }

        /// <summary>
        /// The usage events files parsing report
        /// </summary>
        [JsonProperty("usageEventsParsing")]
        public ParsingReport UsageEventsParsingReport { get; set; }

        /// <summary>
        /// The number of items found in catalog
        /// </summary>
        [JsonProperty("numberOfCatalogItems")]
        public int? NumberOfCatalogItems { get; set; }

        /// <summary>
        /// The number of valid (which are present in catalog if provided) unique items found in usage files 
        /// </summary>
        [JsonProperty("numberOfUsageItems")]
        public int? NumberOfUsageItems { get; set; }

        /// <summary>
        /// The number of unique users found in usage files 
        /// </summary>
        [JsonProperty("numberOfUsers")]
        public int? NumberOfUsers { get; set; }

        /// <summary>
        /// The ratio of unique items found in usage files and total items in catalog
        /// </summary>
        [JsonProperty("catalogCoverage")]
        public double? CatalogCoverage { get; set; }

        /// <summary>
        /// The model evaluation report
        /// </summary>
        [JsonProperty("evaluation")]
        public ModelEvaluationResult EvaluationResult { get; set; }

        /// <summary>
        /// The calculated catalog feature weights
        /// </summary>
        [JsonProperty("catalogFeatureWeights")]
        public IDictionary<string, double> CatalogFeatureWeights { get; set; }
    }
}