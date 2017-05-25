// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

namespace Recommendations.Core.Train
{
    public class ModelTraininigDuration
    {
        /// <summary>
        /// Gets the total model training duration
        /// </summary>
        public TimeSpan TotalDuration { get; private set; }

        /// <summary>
        /// Gets the catalog parsing duration
        /// </summary>
        public TimeSpan CatalogParsingDuration { get; private set; }

        /// <summary>
        /// Gets the usage files parsing duration
        /// </summary>
        public TimeSpan UsageFilesParsingDuration { get; private set; }

        /// <summary>
        /// Gets the core training duration
        /// </summary>
        public TimeSpan TrainingDuration { get; private set; }

        /// <summary>
        /// Gets the usage files parsing duration
        /// </summary>
        public TimeSpan EvaluationUsageFilesParsingDuration { get; private set; }

        /// <summary>
        /// Gets the model evaluation duration
        /// </summary>
        public TimeSpan EvaluationDuration { get; private set; }

        /// <summary>
        /// Gets the duration of storing user history usage events
        /// </summary>
        public TimeSpan? StoringUserHistoryDuration { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="ModelTraininigDuration"/> class.
        /// </summary>
        /// <param name="durationStopwatch">A started stopwatch to measure the durations</param>
        private ModelTraininigDuration(Stopwatch durationStopwatch)
        {
            _durationStopwatch = durationStopwatch;
        }

        /// <summary>
        /// Creates a new <see cref="ModelTraininigDuration"/> instance and start the duration measurement. 
        /// </summary>
        /// <returns></returns>
        internal static ModelTraininigDuration Start()
        {
            return new ModelTraininigDuration(Stopwatch.StartNew());
        }

        /// <summary>
        /// Sets the catalog parsing duration to be the elapsed duration
        /// </summary>
        internal void SetCatalogParsingDuration()
        {
            CatalogParsingDuration = _durationStopwatch.Elapsed;
            _durationStopwatch.Restart();
        }

        /// <summary>
        /// Sets the usage files parsing duration to be the elapsed duration
        /// </summary>
        internal void SetUsageFilesParsingDuration()
        {
            UsageFilesParsingDuration = _durationStopwatch.Elapsed;
            _durationStopwatch.Restart();
        }

        /// <summary>
        /// Sets the core training duration to be the elapsed duration
        /// </summary>
        internal void SetTrainingDuration()
        {
            TrainingDuration = _durationStopwatch.Elapsed;
            _durationStopwatch.Restart();
        }

        /// <summary>
        /// Sets the evaluation usage files parsing duration to be the elapsed duration
        /// </summary>
        internal void SetEvaluationUsageFilesParsingDuration()
        {
            EvaluationUsageFilesParsingDuration = _durationStopwatch.Elapsed;
            _durationStopwatch.Restart();
        }

        /// <summary>
        /// Sets the evaluation duration to be the elapsed duration
        /// </summary>
        internal void SetEvaluationDuration()
        {
            EvaluationDuration = _durationStopwatch.Elapsed;
            _durationStopwatch.Restart();
        }

        /// <summary>
        /// Stops the duration measuring and sets the total duration
        /// </summary>
        internal void Stop()
        {
            _durationStopwatch.Stop();
            TotalDuration = _durationStopwatch.Elapsed + CatalogParsingDuration + UsageFilesParsingDuration +
                            TrainingDuration + EvaluationUsageFilesParsingDuration + EvaluationDuration;
        }

        private readonly Stopwatch _durationStopwatch;
    }
}