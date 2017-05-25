// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Recommendations.Core.Train;

namespace Recommendations.Core.Sar
{
    /// <summary>
    /// Holds the settings for SAR training
    /// </summary>
    internal class SarTrainingSettings
    {
        /// <summary>
        /// Gets or sets the indexed usage file path
        /// </summary>
        public string IndexedUsageFilePath { get; set; }

        /// <summary>
        /// Gets or sets the transformed catalog file path. 
        /// </summary>
        public string TransformedCatalogFilePath { get; set; }
        
        /// <summary>
        /// Gets or sets the core training settings
        /// </summary>
        public ITrainingSettings TrainingSettings { get; set; }

        /// <summary>
        /// Gets or sets the number of users in the user id index file.
        /// </summary>
        public int UniqueUsersCount { get; set; }

        /// <summary>
        /// Gets or sets the number of usage items in the item id index file.
        /// </summary>
        public int UniqueUsageItemsCount { get; set; }
    }
}
