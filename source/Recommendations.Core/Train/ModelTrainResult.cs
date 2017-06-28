// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Recommendations.Core.Evaluate;
using Recommendations.Core.Parsing;
using System.Collections.Generic;

namespace Recommendations.Core.Train
{
    /// <summary>
    /// Represents the result of 
    /// </summary>
    public class ModelTrainResult
    {
        /// <summary>
        /// Gets or set a value indicating whether the model training was completed successfully
        /// </summary>
        public bool IsCompletedSuccessfuly => Model != null;

        /// <summary>
        /// Gets or sets the train result completion message
        /// </summary>
        public string CompletionMessage { get; set; }

        /// <summary>
        /// Gets or sets the trained model
        /// </summary>
        public ITrainedModel Model { get; set; }

        /// <summary>
        /// Gets or sets the usage files parsing report
        /// </summary>
        public FileParsingReport UsageFilesParsingReport { get; set; }

        /// <summary>
        /// Gets or sets the catalog file parsing report
        /// </summary>
        public FileParsingReport CatalogFilesParsingReport { get; set; }

        /// <summary>
        /// Gets or sets the number of items found in catalog files 
        /// </summary>
        public int? CatalogItemsCount { get; set; }

        /// <summary>
        /// Gets or sets the number of unique users found in usage files 
        /// </summary>
        public int UniqueUsersCount { get;set; }

        /// <summary>
        /// Gets or sets the number of unique items found in catalog\usage files 
        /// </summary>
        public int UniqueItemsCount { get; set; }

        /// <summary>
        /// Gets or sets the evaluation files parsing report
        /// </summary>
        public FileParsingReport EvaluationFilesParsingReport { get; set; }

        /// <summary>
        /// Gets or sets Evaluation metrics for a model
        /// </summary>
        public ModelMetrics ModelMetrics { get; set; }

        /// <summary>
        /// Gets or sets the model training duration information
        /// </summary>
        public ModelTraininigDuration Duration { get; set; }

        /// <summary>
        /// Gets or sets the calculated catalog feature weights
        /// </summary>
        public IDictionary<string, double> CatalogFeatureWeights { get; set; }
    }
}
