// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace Recommendations.Core.Recommend
{
    public interface IUsageEvent
    {
        /// <summary>
        /// The item id related to the usage event
        /// </summary>
        string ItemId { get; set; }

        /// <summary>
        /// The usage event timestamp
        /// </summary>
        DateTime? Timestamp { get; set; }

        /// <summary>
        /// The usage event type. Will be ignored is 'weight' is also provided.
        /// </summary>
        UsageEventType? EventType { get; set; }

        /// <summary>
        /// The weight of the event
        /// </summary>
        float? Weight { get; set; }
    }
}