// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Recommendations.Core.Recommend
{
    internal static class UsageEventTypeExtensionMethods
    {
        /// <summary>
        /// Gets the weight associated with an <see cref="UsageEventType"/>.
        /// </summary>
        /// <param name="eventType">The event type to convert</param>
        /// <returns>The weight of the event</returns>
        public static float GetEventWeight(this UsageEventType eventType)
        {
            switch (eventType)
            {
                case UsageEventType.Click:
                    return 1;
                case UsageEventType.RecommendationClick:
                    return 2;
                case UsageEventType.AddShopCart:
                    return 3;
                case UsageEventType.RemoveShopCart:
                    return -1;
                case UsageEventType.Purchase:
                    return 4;
                default:
                    return 1;
            }
        }
    }
}
