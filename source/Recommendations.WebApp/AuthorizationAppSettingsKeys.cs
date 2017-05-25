// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.WebApp
{
    /// <summary>
    /// Authorization related application settings keys
    /// </summary>
    internal static class AuthorizationAppSettingsKeys
    {
        /// <summary>
        /// The application settings key for 'admin' authorization primary key 
        /// </summary>
        public const string AdminPrimaryKey = "AdminPrimaryKey";

        /// <summary>
        /// The application settings key for 'admin' authorization secondary key 
        /// </summary>
        public const string AdminSecondaryKey = "AdminSecondaryKey";

        /// <summary>
        /// The application settings key for 'recommend' authorization primary key 
        /// </summary>
        public const string RecommendPrimaryKey = "RecommendPrimaryKey";

        /// <summary>
        /// The application settings key for 'recommend' authorization secondary key 
        /// </summary>
        public const string RecommendSecondaryKey = "RecommendSecondaryKey";
    }
}