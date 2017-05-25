// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.MachineLearning;
using Microsoft.MachineLearning.Data;
using Microsoft.MachineLearning.EntryPoints;
using Microsoft.MachineLearning.Model;
using Microsoft.MachineLearning.Recommend;
using Recommendations.Core.Recommend;

namespace Recommendations.Core.Train
{
    [Serializable]
    internal class TrainedModel : ITrainedModel, ISerializable
    {
        /// <summary>
        /// Gets the trained model properties
        /// </summary>
        public ModelProperties Properties { get; }

        /// <summary>
        /// Gets the indexed item ids 
        /// </summary>
        public string[] ItemIdIndex { get; }

        /// <summary>
        /// Gets the model recommender data
        /// </summary>
        public ModelRecommenderData RecommenderData { get; }

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="TrainedModel"/> class.
        /// </summary>
        /// <param name="predictorModel">The predictor model</param>
        /// <param name="properties">The model properties</param>
        /// <param name="itemIdIndex">The indexed item ids </param>
        internal TrainedModel(IPredictorModel predictorModel, ModelProperties properties, string[] itemIdIndex)
        {
            if (predictorModel == null)
            {
                throw new ArgumentNullException(nameof(predictorModel));
            }

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (itemIdIndex == null)
            {
                throw new ArgumentNullException(nameof(itemIdIndex));
            }

            _predictorModel = predictorModel;
            RecommenderData = new ModelRecommenderData(_predictorModel.Predictor as IUserHistoryToItemsRecommender);
            Properties = properties;
            ItemIdIndex = itemIdIndex;
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        public TrainedModel(SerializationInfo info, StreamingContext context)
        {
            Properties = (ModelProperties)info.GetValue(nameof(Properties), typeof(ModelProperties));
            ItemIdIndex = (string[])info.GetValue(nameof(ItemIdIndex), typeof(string[]));

            byte[] sarModelBytes = (byte[])info.GetValue(nameof(RecommenderData), typeof(byte[]));
            using (var stream = new MemoryStream(sarModelBytes))
            {
                using (var environment = new TlcEnvironment(verbose:true))
                {
                    IPredictor predictor = ModelFileUtils.LoadPredictorOrNull(environment, stream);
                    RecommenderData = new ModelRecommenderData(predictor as IUserHistoryToItemsRecommender);
                }
            }
        }

        #endregion

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Properties), Properties);
            info.AddValue(nameof(ItemIdIndex), ItemIdIndex);

            using (var stream = new MemoryStream())
            {
                using (var environment = new TlcEnvironment())
                {
                    _predictorModel.Save(environment, stream);
                }

                byte[] predictorBytes = stream.ToArray();
                info.AddValue(nameof(RecommenderData), predictorBytes);
            }
        }

        private readonly IPredictorModel _predictorModel;
    }
}
