// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Recommendations.Common.Api
{
    /// <summary>
    /// Represents a model
    /// </summary>
    public class Model
    {
        /// <summary>
        /// The id of the model.
        /// </summary>
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// The description of the model.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// The creation time of the model.
        /// </summary>
        [JsonProperty("creationTime")]
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// The model status
        /// </summary>
        [JsonProperty("modelStatus"), JsonConverter(typeof(StringEnumConverter))]
        public ModelStatus Status { get; set; }

        /// <summary>
        /// Gets or sets a message associated with the model status
        /// </summary>
        [JsonProperty("modelStatusMessage")]
        public string StatusMessage { get; set; }

        /// <summary>
        /// The model parameters used for training
        /// </summary>
        [JsonProperty("parameters")]
        public ModelTrainingParameters Parameters { get; set; }

        /// <summary>
        /// The model statistics 
        /// </summary>
        [JsonProperty("statistics")]
        public ModelStatistics Statistics { get; set; }
    }
}
