// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Recommendations.Core.Recommend
{
    public enum UsageEventType
    {
        /// <summary>
        /// a custom usage event
        /// </summary>
        Custom = 0,

        /// <summary>
        /// a user clicked on an item 
        /// </summary>
        Click = 1,

        /// <summary>
        /// A user clicked on a recommended item 
        /// </summary>
        RecommendationClick = 2,

        /// <summary>
        /// A user added an item to his shopping cart
        /// </summary>
        AddShopCart = 3,

        /// <summary>
        /// A user added an item to his shopping cart
        /// </summary>
        RemoveShopCart = 4,

        /// <summary>
        /// A user purchased items
        /// </summary>
        Purchase = 5
    }
}