// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Common.Api
{
    /// <summary>
    /// The possible model statuses  
    /// </summary>
    public enum ModelStatus
    {
        /// <summary>
        /// Modeling resources created
        /// </summary>
        Created = 0,

        /// <summary>
        /// Modeling in progress
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Modeling was completed successfully
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Modeling had failed
        /// </summary>
        Failed = 3
    }
}