// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Recommendations.Core.Train;

namespace Recommendations.WebApp.Models
{
    /// <summary>
    /// Represents the parameters of a model
    /// </summary>
    public partial class ModelParameters
    {
        /// <summary>
        /// Model description.
        /// </summary>
        [StringLength(256), DisplayName("description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// The name of a blob container in the default storage account used by the service that stores the modeling files.
        /// </summary>
        [DisplayName("blobContainerName")]
        [CustomValidation(typeof(ModelParameters), nameof(ValidateBlobContainerName))]
        [JsonProperty("blobContainerName")]
        public string BlobContainerName { get; set; }

        /// <summary>
        /// Catalog file path relative to the container.
        /// </summary>
        [DisplayName("catalogFileRelativePath")]
        [CustomValidation(typeof(ModelParameters), nameof(ValidateCatalogBlobRelativePath))]
        [JsonProperty("catalogFileRelativePath")]
        public string CatalogFileRelativePath { get; set; }

        /// <summary>
        /// Usage file\folder path relative to the container.
        /// </summary>
        [Required, DisplayName("usageRelativePath")]
        [CustomValidation(typeof(ModelParameters), nameof(ValidateBlobExistsOrANonEmptyBlobDirectory))]
        [JsonProperty("usageRelativePath")]
        public string UsageRelativePath { get; set; }

        /// <summary>
        /// Optional. Evaluation file\folder path relative to the container.
        /// </summary>
        [CustomValidation(typeof(ModelParameters), nameof(ValidateBlobExistsOrANonEmptyBlobDirectory))]
        [JsonProperty("evaluationUsageRelativePath")]
        public string EvaluationUsageRelativePath { get; set; }

        /// <summary>
        /// How conservative the model is. Number of co-occurrences of items to be considered for modeling.
        /// </summary>
        [Range(3, 50), DisplayName("supportThreshold")]
        [JsonProperty("supportThreshold")]
        public int? SupportThreshold { get; set; }

        /// <summary>
        /// Indicates how to group usage events before counting co-occurrences. 
        /// A 'User' co-occurrence unit will consider all items purchased by the same user as occurring together in the same session. 
        /// A 'Timestamp' co-occurrence unit will consider all items purchased by the same user in the same time as occurring together in the same session.
        /// </summary>
        [JsonProperty("cooccurrenceUnit")]
        public CooccurrenceUnit? CooccurrenceUnit { get; set; }

        /// <summary>
        /// Defines the similarity function to be used by the model. Lift favors serendipity, 
        /// Co-occurrence favors predictability, and Jaccard is a nice compromise between the two.
        /// </summary>
        [JsonProperty("similarityFunction"), JsonConverter(typeof(StringEnumConverter))]
        public SimilarityFunction? SimilarityFunction { get; set; }

        /// <summary>
        /// Indicates if the recommendation should also push cold items via feature similarity.
        /// </summary>
        [DisplayName("enableColdItemPlacement")]
        [JsonProperty("enableColdItemPlacement")]
        public bool? EnableColdItemPlacement { get; set; }

        /// <summary>
        /// Indicates whether the similarity between pairs of cold items (catalog items without usage) should be computed.
        /// If set to false, only similarity between cold and warm item will be computed, using catalog item features. 
        /// Note that this configuration is only relevant when enableColdItemPlacement is set to true. 
        /// </summary>
        [DisplayName("enableColdToColdRecommendations")]
        [JsonProperty("enableColdToColdRecommendations")]
        public bool? EnableColdToColdRecommendations { get; set; }

        /// <summary>
        /// For user-to-item recommendations, it defines whether the event type and the time of the event should be considered as 
        /// input into the scoring. 
        /// </summary>
        [JsonProperty("enableUserAffinity")]
        public bool? EnableUserAffinity { get; set; }

        /// <summary>
        /// Enables user to item recommendations by storing the usage events per user and using it for recommendations.
        /// Setting this to true will impact the performance of the training process.
        /// </summary>
        [JsonProperty("enableUserToItemRecommendations")]
        public bool? EnableUserToItemRecommendations { get; set; }

        /// <summary>
        /// Allow seed items (input items to the recommendation request) to be returned as part of the recommendation results.
        /// </summary>
        [JsonProperty("allowSeedItemsInRecommendations")]
        public bool? AllowSeedItemsInRecommendations { get; set; }

        /// <summary>
        /// Backfill recommendations with popular items.
        /// </summary>
        [JsonProperty("enableBackfilling")]
        public bool? EnableBackfilling { get; set; }

        /// <summary>
        /// The decay period in days. The strength of the signal for events that are that many days old will be half that of the most recent events.
        /// </summary>
        [Range(1, int.MaxValue), DisplayName("decayPeriodInDays")]
        [JsonProperty("decayPeriodInDays")]
        public int? DecayPeriodInDays { get; set; }
    }
}