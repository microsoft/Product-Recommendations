// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.MachineLearning.Api;

namespace Recommendations.Core.Sar
{
    internal class SarCatalogItem
    {
        /// <summary>
        /// The catalog item id
        /// </summary>
        [ColumnName("Item")]
        public uint ItemId;

        /// <summary>
        /// An array of values of all the catalog features (values may be empty)
        /// </summary>
        [ColumnName("Features")]
        public string[] FeatureVector;
    }
}
