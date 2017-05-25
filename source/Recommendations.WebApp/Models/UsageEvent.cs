// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Recommendations.Core.Recommend;

namespace Recommendations.WebApp.Models
{
    /// <summary>
    /// Represent a single item usage event
    /// </summary>
    public class UsageEvent : IUsageEvent
    {
        /// <summary>
        /// The item id related to the usage event
        /// </summary>
        [JsonProperty("itemId")]
        public string ItemId { get; set; }

        /// <summary>
        /// The usage event timestamp
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime? Timestamp { get; set; }
        
        /// <summary>
        /// The usage event type. Will be ignored if 'weight' is also provided.
        /// </summary>
        [JsonProperty("eventType"), JsonConverter(typeof(StringEnumConverter))]
        public UsageEventType? EventType { get; set; }

        /// <summary>
        /// Gets or sets the event weight.
        /// </summary>
        [JsonProperty("weight")]
        public float? Weight { get; set; }
    }
}