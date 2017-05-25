// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.MachineLearning.Api;

namespace Recommendations.Core.Sar
{
    /// <summary>
    /// Holds the usage data for a user in UserId,ItemId format
    /// </summary>
    internal class SarEvaluationUsageEvent
    {
        /// <summary>
        /// The user id
        /// </summary>
        [ColumnName("User")]
        public string UserId;

        /// <summary>
        /// The item id
        /// </summary>
        [ColumnName("Item")]
        public string ItemId;
    }
}