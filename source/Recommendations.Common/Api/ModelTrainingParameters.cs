// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Recommendations.Core.Train;

namespace Recommendations.Common.Api
{
    /// <summary>
    /// Parameters for training a new model
    /// </summary>
    public class ModelTrainingParameters : IModelTrainerSettings
    {
        /// <summary>
        /// Gets or sets the blob container name
        /// </summary>
        [JsonProperty("blobContainerName")]
        public string BlobContainerName { get; set; }

        /// <summary>
        /// Catalog file path relative to the container.
        /// </summary>
        [JsonProperty("catalogFileRelativePath")]
        public string CatalogFileRelativePath { get; set; }

        /// <summary>
        /// Usage file\folder path relative to the container.
        /// </summary>
        [JsonProperty("usageRelativePath")]
        public string UsageRelativePath { get; set; }

        /// <summary>
        /// Optional. Evaluation file\folder path relative to the container.
        /// </summary>
        [JsonProperty("evaluationUsageRelativePath")]
        public string EvaluationUsageRelativePath { get; set; }

        /// <summary>
        /// How conservative the model is. Number of co-occurrences of items to be considered for modeling.
        /// </summary>
        [JsonProperty("supportThreshold")]
        public int SupportThreshold { get; set; }

        /// <summary>
        /// Indicates how to group usage events before counting co-occurrences. 
        /// A 'User' co-occurrence unit will consider all items purchased by the same user as occurring together in the same session. 
        /// A 'Timestamp' co-occurrence unit will consider all items purchased by the same user in the same time as occurring together in the same session.
        /// </summary>
        [JsonProperty("cooccurrenceUnit"), JsonConverter(typeof(StringEnumConverter))]
        public CooccurrenceUnit CooccurrenceUnit { get; set; }

        /// <summary>
        /// Defines the similarity function to be used by the model. Lift favors serendipity, 
        /// Co-occurrence favors predictability, and Jaccard is a nice compromise between the two.
        /// </summary>
        [JsonProperty("similarityFunction"), JsonConverter(typeof(StringEnumConverter))]
        public SimilarityFunction SimilarityFunction { get; set; }

        /// <summary>
        /// Indicates if the recommendation should also push cold items via feature similarity.
        /// </summary>
        [JsonProperty("enableColdItemPlacement")]
        public bool EnableColdItemPlacement { get; set; }

        /// <summary>
        /// Indicates whether the similarity between pairs of cold items (catalog items without usage) should be computed.
        /// If set to false, only similarity between cold and warm item will be computed, using catalog item features. 
        /// Note that this configuration is only relevant when enableColdItemPlacement is set to true. 
        /// </summary>
        [JsonProperty("enableColdToColdRecommendations")]
        public bool EnableColdToColdRecommendations { get; set; }

        /// <summary>
        /// For user-to-item recommendations, it defines whether the event type and the time of the event should be considered as 
        /// input into the scoring. 
        /// </summary>
        [JsonProperty("enableUserAffinity")]
        public bool EnableUserAffinity { get; set; }

        /// <summary>
        /// Enables user to item recommendations by storing the usage events per user and using it for recommendations.
        /// Setting this to true will impact the performance of the training process.
        /// </summary>
        [JsonProperty("enableUserToItemRecommendations")]
        public bool EnableUserToItemRecommendations { get; set; }

        /// <summary>
        /// Allow seed items (input items to the recommendation request) to be returned as part of the recommendation results.
        /// </summary>
        [JsonProperty("allowSeedItemsInRecommendations")]
        public bool AllowSeedItemsInRecommendations { get; set; }

        /// <summary>
        /// Backfill recommendations with popular items.
        /// </summary>
        [JsonProperty("enableBackfilling")]
        public bool EnableBackfilling { get; set; }

        /// <summary>
        /// The decay period in days. The strength of the signal for events that are that many days old will be half that of the most recent events.
        /// </summary>
        [JsonProperty("decayPeriodInDays")]
        public int DecayPeriodInDays { get; set; }

        /// <summary>
        /// Gets a string that represents the training parameters
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return
                $"{nameof(BlobContainerName)}: {BlobContainerName}, " +
                $"{nameof(CatalogFileRelativePath)}: {CatalogFileRelativePath}, " +
                $"{nameof(UsageRelativePath)}: {UsageRelativePath}, " +
                $"{nameof(EvaluationUsageRelativePath)}: {EvaluationUsageRelativePath}, " +
                $"{nameof(SupportThreshold)}: {SupportThreshold}, " +
                $"{nameof(CooccurrenceUnit)}: {CooccurrenceUnit}, " +
                $"{nameof(SimilarityFunction)}: {SimilarityFunction}, " +
                $"{nameof(EnableColdToColdRecommendations)}: {EnableColdToColdRecommendations}, " +
                $"{nameof(EnableUserToItemRecommendations)}: {EnableUserToItemRecommendations}, " +
                $"{nameof(AllowSeedItemsInRecommendations)}: {AllowSeedItemsInRecommendations}, " +
                $"{nameof(EnableBackfilling)}: {EnableBackfilling}, " +
                $"{nameof(DecayPeriodInDays)}: {DecayPeriodInDays}";
        }

        /// <summary>
        /// Gets an instance of <see cref="ModelTrainingParameters"/> with the default parameters
        /// </summary>
        public static ModelTrainingParameters Default => new ModelTrainingParameters
        {
            SupportThreshold = 6,
            CooccurrenceUnit = CooccurrenceUnit.User,
            SimilarityFunction = SimilarityFunction.Jaccard,
            EnableColdItemPlacement = false,
            EnableColdToColdRecommendations = false,
            EnableUserAffinity = true,
            EnableUserToItemRecommendations = false,
            AllowSeedItemsInRecommendations = false,
            EnableBackfilling = true,
            DecayPeriodInDays = 30
        };
    }
}
