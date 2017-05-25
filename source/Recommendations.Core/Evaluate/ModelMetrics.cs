// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Recommendations.Core.Evaluate
{
    /// <summary>
    /// A model precision and diversity metrics
    /// </summary>
    public class ModelMetrics
    {
        /// <summary>
        /// Precision@K metrics for a model. These are a measure of quality of the Model. It works by splitting the input 
        /// data into a test and training data. Then use the test period to evaluate what percentage of the customers
        /// would have actually clicked on a recommendation if k recommendations had been shown to them given their prior history.
        /// </summary>
        [JsonProperty("precisionMetrics")]
        public IList<PrecisionMetric> ModelPrecisionMetrics { get; set; }
        
        /// <summary>
        /// Diversity metrics for a model
        /// </summary>
        [JsonProperty("diversityMetrics")]
        public ModelDiversityMetrics ModelDiversityMetrics { get; set; }
    }
}