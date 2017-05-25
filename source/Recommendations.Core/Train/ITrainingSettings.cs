// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Core.Train
{
    public interface ITrainingSettings
    {
        /// <summary>
        /// How conservative the model is. Number of co-occurrences of items to be considered for modeling.
        /// </summary>
        int SupportThreshold { get; }
        
        /// <summary>
        /// Defines the similarity function to be used by the model. Lift favors serendipity, 
        /// Co-occurrence favors predictability, and Jaccard is a nice compromise between the two.
        /// </summary>
        SimilarityFunction SimilarityFunction { get; }

        /// <summary>
        /// Indicates if the recommendation should also push cold items via feature similarity.
        /// </summary>
        bool EnableColdItemPlacement { get; }

        /// <summary>
        /// Indicates whether the similarity between pairs of cold items (catalog items without usage) should be computed.
        /// If set to false, only similarity between cold and warm item will be computed, using catalog item features. 
        /// Note that this configuration is only relevant when enableColdItemPlacement is set to true. 
        /// </summary>
        bool EnableColdToColdRecommendations { get; }

        /// <summary>
        /// Backfill recommendations with popular items.
        /// </summary>
        bool EnableBackfilling { get; }
    }
}