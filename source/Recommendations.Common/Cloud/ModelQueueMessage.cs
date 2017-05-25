// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Newtonsoft.Json;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// Represents the message sent to the model training queue
    /// </summary>
    public class ModelQueueMessage
    {
        /// <summary>
        /// Gets or sets the model id to train
        /// </summary>
        [JsonProperty("modelId")]
        public Guid ModelId { get; set; }
    }
}
