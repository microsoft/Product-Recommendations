// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// Stores a model id as an Azure table entity
    /// </summary>
    public class ModelIdTableEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the model id
        /// </summary>
        public Guid ModelId { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelIdTableEntity"/> class
        /// </summary>
        /// <param name="rowKey">The entity row key to use</param>
        /// <param name="modelId">The model id</param>
        public ModelIdTableEntity(string rowKey, Guid modelId) : base(typeof(ModelIdTableEntity).Name, rowKey)
        {
            ModelId = modelId;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelIdTableEntity"/> class
        /// </summary>
        public ModelIdTableEntity() : this(null, Guid.Empty)
        {
        }
    }
}
