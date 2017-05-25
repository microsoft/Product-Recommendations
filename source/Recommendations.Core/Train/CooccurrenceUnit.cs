// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Core.Train
{
    /// <summary>
    /// The types of usage event grouping when counting co-occurrences
    /// </summary>
    public enum CooccurrenceUnit
    {
        /// <summary>
        /// Considers all items purchased by the same user as 
        /// occurring together in the same session 
        /// </summary>
        User = 0,

        /// <summary>
        /// Considers all items purchased by the same user and at 
        /// the same time as occurring together in the same session
        /// </summary>
        Timestamp = 1
    }
}
