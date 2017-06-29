// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.MachineLearning;
using Microsoft.MachineLearning.Api;
using Microsoft.MachineLearning.Data;
using Microsoft.MachineLearning.EntryPoints;
using Microsoft.MachineLearning.Recommend.ItemSimilarity;
using Recommendations.Core.Train;
using SAR = Microsoft.MachineLearning.Recommend.Sar.Sar;

namespace Recommendations.Core.Sar
{
    internal class SarTrainer
    {
        /// <summary>
        /// Gets the features weights as calculated by the training process or empty list 
        /// if no features were found
        /// </summary>
        public IDictionary<string, double> FeatureWeights { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="SarTrainer"/> class.
        /// </summary>
        /// <param name="tracer">A message tracer to use for logging</param>
        public SarTrainer(ITracer tracer = null)
        {
            _tracer = tracer ?? new DefaultTracer();
        }

        /// <summary>
        /// Trains a model using SAR.
        /// </summary>
        /// <param name="settings">The training settings</param>
        /// <param name="usageEvents">The usage events to use for training</param>
        /// <param name="catalogItems">The catalog items to use for training</param>
        /// <param name="featureNames">The names of the catalog items features, in the same order as the feature values in the catalog</param>
        /// <param name="uniqueUsersCount">The number of users in the user id index file.</param>
        /// <param name="uniqueUsageItemsCount">The number of usage items in the item id index file</param>
        /// <param name="catalogFeatureWeights">The computed catalog items features weights (if relevant)</param>
        public IPredictorModel Train(ITrainingSettings settings,
            IList<SarUsageEvent> usageEvents,
            IList<SarCatalogItem> catalogItems,
            string[] featureNames,
            int uniqueUsersCount,
            int uniqueUsageItemsCount,
            out IDictionary<string, double> catalogFeatureWeights)
        {
            return Train(settings, usageEvents, catalogItems, featureNames, uniqueUsersCount, uniqueUsageItemsCount,
                out catalogFeatureWeights, CancellationToken.None);
        }

        /// <summary>
        /// Trains a model using SAR.
        /// </summary>
        /// <param name="settings">The training settings</param>
        /// <param name="usageEvents">The usage events to use for training</param>
        /// <param name="catalogItems">The catalog items to use for training</param>
        /// <param name="featureNames">The names of the catalog items features, in the same order as the feature values in the catalog</param>
        /// <param name="uniqueUsersCount">The number of users in the user id index file.</param>
        /// <param name="uniqueUsageItemsCount">The number of usage items in the item id index file</param>
        /// <param name="catalogFeatureWeights">The computed catalog items features weights (if relevant)</param>
        /// <param name="cancellationToken">A cancellation token</param>
        public IPredictorModel Train(ITrainingSettings settings,
            IList<SarUsageEvent> usageEvents,
            IList<SarCatalogItem> catalogItems,
            string[] featureNames,
            int uniqueUsersCount,
            int uniqueUsageItemsCount,
            out IDictionary<string, double> catalogFeatureWeights,
            CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (usageEvents == null)
            {
                throw new ArgumentNullException(nameof(usageEvents));
            }

            if (settings.EnableColdItemPlacement && catalogItems == null)
            {
                throw new ArgumentNullException(nameof(catalogItems));
            }

            if (uniqueUsersCount < 0)
            {
                var exception = new ArgumentException($"{nameof(uniqueUsersCount)} must be a positive integer");
                _tracer.TraceWarning(exception.ToString());
                throw exception;
            }

            if (uniqueUsageItemsCount < 0)
            {
                var exception = new ArgumentException($"{nameof(uniqueUsageItemsCount)} must be a positive integer");
                _tracer.TraceWarning(exception.ToString());
                throw exception;
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (TlcEnvironment environment = new TlcEnvironment(verbose: true))
            {
                _detectedFeatureWeights = null;
                try
                {
                    environment.AddListener<ChannelMessage>(ChannelMessageListener);
                    IHost environmentHost = environment.Register("SarHost");

                    // bind the cancellation token to SAR cancellation
                    using (cancellationToken.Register(() => { environmentHost.StopExecution(); }))
                    {
                        _tracer.TraceInformation("Starting training model using SAR");
                        IPredictorModel model = TrainModel(environmentHost, settings, usageEvents, catalogItems, uniqueUsersCount,
                            uniqueUsageItemsCount);

                        catalogFeatureWeights = new Dictionary<string, double>();
                        if (_detectedFeatureWeights != null && featureNames != null)
                        {
                            if ( _detectedFeatureWeights.Length == featureNames.Length)
                            {
                                for (int i = 0; i < featureNames.Length; i++)
                                {
                                    catalogFeatureWeights[featureNames[i]] = _detectedFeatureWeights[i];
                                }
                            }
                            else
                            {
                                _tracer.TraceWarning(
                                    $"Found a mismatch between number of feature names ({featureNames.Length}) and the number of feature weights ({_detectedFeatureWeights.Length})");
                            }
                        }

                        return model;
                    }
                }
                finally
                {
                    environment.RemoveListener<ChannelMessage>(ChannelMessageListener);
                }
            }
        }
        
        /// <summary>
        /// Trains a model using SAR.
        /// </summary>
        private IPredictorModel TrainModel(
            IHost environment,
            ITrainingSettings settings,
            IList<SarUsageEvent> usageItems,
            IList<SarCatalogItem> catalogItems,
            int uniqueUsersCount,
            int uniqueUsageItemsCount)
        {
            IDataView catalog = null;
            if (settings.EnableColdItemPlacement)
            {
                SarCatalogItem item = catalogItems.FirstOrDefault();
                int featuresVectorSize = item?.FeatureVector.Length ?? 0;

                _tracer.TraceInformation($"Found catalog item features vector size of {featuresVectorSize}");

                // check if the catalog has items with any features. if not, there is no point to compute 'cold' items recommendations
                if (featuresVectorSize > 0)
                {
                    _tracer.TraceInformation($"Creating catalog item schema using the features vector size of {featuresVectorSize}");
                    
                    var catalogItemSchema = SchemaDefinition.Create(typeof(SarCatalogItem));
                    catalogItemSchema["Item"].ColumnType = new KeyType(DataKind.U4, 1, uniqueUsageItemsCount);
                    catalogItemSchema["Features"].ColumnType = new VectorType(TextType.Instance, featuresVectorSize);
                    catalog = environment.CreateDataView(catalogItems, catalogItemSchema);
                }
            }

            _tracer.TraceInformation("Creating usage item schema");
            var usageItemSchema = SchemaDefinition.Create(typeof(SarUsageEvent));
            usageItemSchema["user"].ColumnType = new KeyType(DataKind.U4, 1, uniqueUsersCount);
            usageItemSchema["Item"].ColumnType = new KeyType(DataKind.U4, 1, uniqueUsageItemsCount);

            // create a usage data view
            IDataView usage = environment.CreateDataView(usageItems, usageItemSchema);

            // set the similarity function factory
            ISimCalculatorFactory simCalculatorFactory;
            switch (settings.SimilarityFunction)
            {
                case SimilarityFunction.Jaccard:
                    simCalculatorFactory = new JaccardSimilarityCalculator.Arguments
                    {
                        threshold = settings.SupportThreshold
                    };
                    break;
                case SimilarityFunction.Lift:
                    simCalculatorFactory = new LiftSimilarityCalculator.Arguments
                    {
                        threshold = settings.SupportThreshold
                    };
                    break;
                case SimilarityFunction.Cooccurrence:
                    simCalculatorFactory = new CoOccurrenceSimilarityCalculator.Arguments
                    {
                        threshold = settings.SupportThreshold
                    };
                    break;
                default:
                    var exception = new ArgumentException($"Unknown similarity function '{settings.SimilarityFunction}'");
                    _tracer.TraceError(exception.ToString());
                    throw exception;
            }

            // prepare SAR trainer's input arguments
            var trainInput = new SAR.Input
            {
                TrainingData = usage,
                CatalogData = catalog,
                Calculator = simCalculatorFactory,
                Backfill = settings.EnableBackfilling,
                ItemColumn = "Item",
                UserColumn = "user",
                ColdToCold = settings.EnableColdToColdRecommendations,
                MaxColdItems = 10,
                MultiValueFeatures = true
            };

            _tracer.TraceInformation("Training a SAR predictor using the usage and catalog data");
            SAR.Output trainOutput = SAR.Train(environment, trainInput);
            return trainOutput.PredictorModel;
        }

        /// <summary>
        /// Listens to channel messages, log them and try to detect feature weights as they are being provided as channel message. 
        /// </summary>
        private void ChannelMessageListener(IMessageSource source, ChannelMessage channelMessage)
        {
            // trace the message
            _tracer.TraceChannelMessage(source, channelMessage);

            // if not already detected, look for 'ColdItemSimilarity' message containing the feature weights 
            // message should be of format: "Cold item feature weights: Cold items, bias=-4.227643, feature weights=0.2925752, 0.1017576, 0.1727819"
            if (_detectedFeatureWeights == null && (source?.FullName?.Contains("Cold item feature weights") ?? false) && channelMessage.Message != null)
            {
                const string featureWeightsPrefix = "feature weights=";
                int featureWeightsIndex = channelMessage.Message.LastIndexOf(featureWeightsPrefix, StringComparison.InvariantCultureIgnoreCase);
                if (featureWeightsIndex > 0)
                {
                    _detectedFeatureWeights =
                        channelMessage.Message?.Substring(featureWeightsIndex + featureWeightsPrefix.Length)
                            .Split(',')
                            .Select(str =>
                            {
                                double weight;
                                return double.TryParse(str, out weight) ? weight : -1;
                            })
                            .ToArray();
                }
            }
        }

        private double[] _detectedFeatureWeights;
        private readonly ITracer _tracer;
    }
}
