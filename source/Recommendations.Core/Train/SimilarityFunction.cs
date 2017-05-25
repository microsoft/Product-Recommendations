// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Core.Train
{
    /// <summary>
    /// The different supported similarity functions 
    /// </summary>
    public enum SimilarityFunction
    {
        /// <summary>
        /// 'Jaccard' similarity function
        /// </summary>
        Jaccard = 0,

        /// <summary>
        /// Co-occurrence similarity function
        /// </summary>
        Cooccurrence = 1,

        /// <summary>
        /// 'Lift' similarity function
        /// </summary>
        Lift = 2
    }
}
