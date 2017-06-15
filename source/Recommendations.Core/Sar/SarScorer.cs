// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Microsoft.MachineLearning;
using Microsoft.MachineLearning.Api;
using Microsoft.MachineLearning.Data;
using Microsoft.MachineLearning.Recommend;

namespace Recommendations.Core.Sar
{
    internal class SarScorer : IDisposable
    {
        /// <summary>
        /// Creates a new instance of <see cref="SarScorer"/> class.
        /// </summary>
        /// <param name="recommender">A trained SAR recommender</param>
        /// <param name="tracer">A message tracer to use for logging</param>
        public SarScorer(IUserHistoryToItemsRecommender recommender, ITracer tracer = null)
        {
            if (recommender == null)
            {
                throw new ArgumentNullException(nameof(recommender));
            }

            _recommender = recommender;
            _tracer = tracer ?? new DefaultTracer();

            // create the input schema
            _usageDataSchema = SchemaDefinition.Create(typeof(SarUsageEvent));
            _usageDataSchema["user"].ColumnType = _recommender.UserIdType;
            _usageDataSchema["Item"].ColumnType = _recommender.ItemIdType;

            // create an environment and register a message listener
            _environment = new TlcEnvironment(verbose: true);
            _environment.AddListener<ChannelMessage>(_tracer.TraceChannelMessage);
        }
        
        /// <summary>
        /// Scores the input usage items.
        /// </summary>
        /// <param name="usageItems">The items to score</param>
        /// <param name="scoringArguments">The scoring arguments</param>
        /// <returns></returns>
        public IEnumerable<SarScoreResult> ScoreUsageItems(IList<SarUsageEvent> usageItems, SarScoringArguments scoringArguments)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(SarScorer).FullName);
            }

            if (usageItems == null)
            {
                throw new ArgumentNullException(nameof(usageItems));
            }

            if (scoringArguments == null)
            {
                throw new ArgumentNullException(nameof(scoringArguments));
            }

            _tracer.TraceVerbose("Getting or creating the prediction engine");
            BatchPredictionEngine<SarUsageEvent, SarScoreResult> engine =
                GetOrCreateBatchPredictionEngine(usageItems, scoringArguments);

            _tracer.TraceInformation($"Getting recommendation for {usageItems.Count} input items");
            lock (engine)
            {
                return engine.Predict(usageItems, false)
                    .OrderByDescending(x => x.Score)
                    .Take(scoringArguments.RecommendationCount)
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets a cached prediction engine or creates a new one if not cached
        /// </summary>
        private BatchPredictionEngine<SarUsageEvent, SarScoreResult> GetOrCreateBatchPredictionEngine(
            IList<SarUsageEvent> usageItems, SarScoringArguments sarScoringArguments)
        {
            var arguments = new RecommenderScorerTransform.Arguments
            {
                // round the recommendations count to optimize engine cache
                recommendationCount = GetRoundedNumberOfResults(sarScoringArguments.RecommendationCount),
                includeHistory = sarScoringArguments.IncludeHistory
            };

            // create a data column mapping
            var dataColumnMapping = new Dictionary<RoleMappedSchema.ColumnRole, string>
            {
                {new RoleMappedSchema.ColumnRole("Item"), "Item"},
                {new RoleMappedSchema.ColumnRole("User"), "user"}
            };

            string weightColumn = null;
            if (sarScoringArguments.ReferenceDate.HasValue)
            {
                // rounding the reference date to the beginning of next day to optimize engine cache
                DateTime referenceDate = sarScoringArguments.ReferenceDate.Value.Date + TimeSpan.FromDays(1);
                arguments.referenceDate = referenceDate.ToString("s");
                if (sarScoringArguments.Decay.HasValue)
                {
                    arguments.decay = sarScoringArguments.Decay.Value.TotalDays;
                }

                dataColumnMapping.Add(new RoleMappedSchema.ColumnRole("Date"), "date");
                weightColumn = "weight";
            }

            // create an engine cache key
            string cacheKey = $"{arguments.recommendationCount}|{arguments.includeHistory}|{arguments.referenceDate}|{arguments.decay}";

            _tracer.TraceVerbose("Trying to find the engine in the cache");
             var engine = _enginesCache.Get(cacheKey) as BatchPredictionEngine<SarUsageEvent, SarScoreResult>;
            if (engine == null)
            {
                _tracer.TraceInformation("Engine is not cached - creating a new engine");
                IDataView pipeline = _environment.CreateDataView(usageItems, _usageDataSchema);
                RoleMappedData usageDataMappedData = _environment.CreateExamples(pipeline, null, weight: weightColumn, custom: dataColumnMapping);
                ISchemaBindableMapper mapper = RecommenderScorerTransform.Create(_environment, arguments, _recommender);
                ISchemaBoundMapper boundMapper = mapper.Bind(_environment, usageDataMappedData.Schema);
                IDataScorerTransform scorer = RecommenderScorerTransform.Create(
                    _environment, arguments, pipeline, boundMapper, null);
                engine = _environment.CreateBatchPredictionEngine<SarUsageEvent, SarScoreResult>(scorer, false, _usageDataSchema);

                bool result = _enginesCache.Add(cacheKey, engine, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) });
                _tracer.TraceVerbose($"Addition of engine to the cache resulted with '{result}'");
            }

            return engine;
        }

        private static int GetRoundedNumberOfResults(int numberOfResults)
        {
            // if less than 5, round the requested number of results to 5, 
            // otherwise round up to the closest multiplication of 10
            if (numberOfResults <= 5)
            {
                return 5;
            }

            // round up to the closest multiplication of 10
            // Examples: 
            //    7 -> 10
            //   10 -> 10
            //   11 -> 20
            //   19 -> 20
            //   21 -> 30
            return (int) Math.Ceiling((double) numberOfResults/10)*10;
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _environment.Dispose();
                    _enginesCache.Dispose();
                    _disposed = true;
                }
            }
        }
        
        private bool _disposed;
        private readonly TlcEnvironment _environment;
        private readonly SchemaDefinition _usageDataSchema;
        private readonly MemoryCache _enginesCache = new MemoryCache(nameof(_enginesCache));
        private readonly IUserHistoryToItemsRecommender _recommender;
        private readonly ITracer _tracer;
    }
}