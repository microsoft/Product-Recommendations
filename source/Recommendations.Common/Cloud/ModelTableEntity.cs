// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Recommendations.Common.Api;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// A models table entity 
    /// </summary>
    public class ModelTableEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the model description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The creation time of the model.
        /// </summary>
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the serialized model status
        /// </summary>
        public string ModelStatus { get; set; }

        /// <summary>
        /// Gets or sets a message associated with the model status
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the serialized model parameters used for training
        /// </summary>
        public string ModelParameters { get; set; }

        /// <summary>
        /// Gets or sets the serialized model statistics 
        /// </summary>
        public string ModelStatistics { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelTableEntity"/> class
        /// </summary>
        /// <param name="modelId">The model id</param>
        /// <param name="modelParameters">The training parameters of the model</param>
        /// <param name="modelStatistics">The model training statistics</param>
        public ModelTableEntity(Guid modelId, ModelTrainingParameters modelParameters = null, ModelStatistics modelStatistics = null) 
            : this()
        {
            RowKey = modelId.ToString();

            if (modelParameters != null)
            {
                ModelParameters = JsonConvert.SerializeObject(modelParameters);
            }

            if (modelStatistics != null)
            {
                ModelStatistics = JsonConvert.SerializeObject(modelStatistics);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelTableEntity"/> class
        /// </summary>
        public ModelTableEntity()
        {
            PartitionKey = nameof(ModelTableEntity);
        }
    }
}
