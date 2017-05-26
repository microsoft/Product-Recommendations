// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Recommendations.Core.Sar;
using Recommendations.Core.Train;

namespace Recommendations.Core.Recommend
{
    public class Recommender : IDisposable
    {
        /// <summary>
        /// Creates a new instance of the <see cref="Recommender"/> class.
        /// </summary>
        /// <param name="trainedModel">A trained model</param>
        /// <param name="documentStore">A document store for storing user history is user-to-item is enabled</param>
        /// <param name="tracer">A message tracer to use for logging</param>
        public Recommender(ITrainedModel trainedModel, IDocumentStore documentStore = null, ITracer tracer = null)
        {
            if (trainedModel == null)
            {
                throw new ArgumentNullException(nameof(trainedModel));
            }

            if (trainedModel.Properties == null)
            {
                throw new ArgumentException("Trained model properties can not be null", nameof(trainedModel.Properties));
            }

            if (trainedModel.ItemIdIndex == null)
            {
                throw new ArgumentException("Trained model item index can not be null", nameof(trainedModel.ItemIdIndex));
            }

            // create a tracer if not provided
            _tracer = tracer ?? new DefaultTracer();

            // create a SAR scorer
            _scorer = new SarScorer(trainedModel.RecommenderData?.Recommender, _tracer);
            _properties = trainedModel.Properties;
            _itemIdIndex = trainedModel.ItemIdIndex;

            if (documentStore != null)
            {
                _userHistoryStore = new UserHistoryStore(documentStore, _properties.UniqueUsersCount, _tracer);
            }

            // create a reverse item id lookup 
            _itemIdReverseLookup =
                _itemIdIndex.Select((id, index) => new { id, index })
                    .ToDictionary(x => x.id, x => (uint)x.index + 1);
        }

        /// <summary>
        /// Gets recommendations using the input <see cref="IUsageEvent"/> instances.
        /// </summary>
        /// <param name="usageEvents">The usage events to consider when getting recommendations</param>
        /// <param name="userId">An optional user id to get recommendations for. If provided, the user's usage events history will also be used</param>
        /// <param name="recommendationCount">The number of requested recommendations</param>
        /// <returns>A list of <see cref="Recommendation"/> items</returns>
        public IList<Recommendation> GetRecommendations(IEnumerable<IUsageEvent> usageEvents, string userId, int recommendationCount)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(Recommender).FullName);
            }
            
            if (recommendationCount <= 0)
            {
                _tracer.TraceVerbose($"Requested '{recommendationCount}' recommendations - returning empty result");
                return new Recommendation[0];
            }
            
            try
            {
                _tracer.TraceVerbose("Converting events to SAR format");
                List<SarUsageEvent> sarUsageEvents =
                    usageEvents?.Where(e => e?.ItemId != null).Select(ConvertToSarUsageEvent).ToList()
                    ?? new List<SarUsageEvent>();
                
                // get user history if user to item recommendations is supported and a user id was provided
                if (!string.IsNullOrWhiteSpace(userId) && _properties.IsUserToItemRecommendationsSupported && _userHistoryStore != null)
                {
                    _tracer.TraceVerbose($"Getting user history usage events for user {userId}");
                    IList<SarUsageEvent> userHistory = _userHistoryStore
                        .GetUserHistory(userId.ToLowerInvariant()).ToList();

                    _tracer.TraceInformation($"Found {userHistory.Count} user history usage events for user {userId}");
                    sarUsageEvents.AddRange(userHistory);
                }

                if (!sarUsageEvents.Any())
                {
                    _tracer.TraceVerbose("No usage events provided - padding with a default usage event");
                    sarUsageEvents.Add(new SarUsageEvent {ItemId = 0});
                }
                
                // score the usage events
                IEnumerable<SarScoreResult> scoreResults = ScoreUsageEvents(sarUsageEvents, recommendationCount);

                _tracer.TraceVerbose("Converting score result to recommendations");
                return scoreResults.Select(ConvertToRecommendation).ToArray();
            }
            catch (Exception ex)
            {
                var exception = new Exception("Exception while trying to get recommendations", ex);
                _tracer.TraceError(exception.ToString());
                throw exception;
            }
        }
        
        internal IList<SarScoreResult> ScoreUsageEvents(IList<SarUsageEvent> sarUsageEvents, int recommendationCount)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(Recommender).FullName);
            }

            if (sarUsageEvents == null)
            {
                throw new ArgumentNullException(nameof(sarUsageEvents));
            }

            // create scoring arguments
            var sarScoringArguments = new SarScoringArguments
            {
                RecommendationCount = recommendationCount,
                IncludeHistory = _properties.IncludeHistory
            };

            if (_properties.EnableUserAffinity)
            {
                DateTime now = DateTime.UtcNow;
                DateTime settingsReferenceDate = _properties.ReferenceDate ?? now;
                DateTime maxTimestampFromUsageEvents = sarUsageEvents.Max(e => (DateTime?)e.Timestamp) ?? now;

                sarScoringArguments.ReferenceDate = settingsReferenceDate < maxTimestampFromUsageEvents
                    ? maxTimestampFromUsageEvents
                    : settingsReferenceDate;

                if (_properties.Decay.HasValue && _properties.Decay.Value.TotalDays > 0)
                {
                    sarScoringArguments.Decay = _properties.Decay;
                }
            }

            _tracer.TraceInformation($"Scoring {sarUsageEvents.Count} usage events");
            IEnumerable<SarScoreResult> scoreResults = _scorer.ScoreUsageItems(sarUsageEvents, sarScoringArguments);
            return scoreResults.ToArray();
        }

        private SarUsageEvent ConvertToSarUsageEvent(IUsageEvent usageEvent)
        {
            var sarUsageEvent = new SarUsageEvent();

            uint itemId;
            if (_itemIdReverseLookup.TryGetValue(usageEvent.ItemId.ToLowerInvariant(), out itemId))
            {
                sarUsageEvent.ItemId = itemId;
            }
            
            if (_properties.EnableUserAffinity)
            {
                sarUsageEvent.Timestamp = usageEvent.Timestamp ?? DateTime.UtcNow;
                sarUsageEvent.Weight = usageEvent.EventType?.GetEventWeight() ?? 1;

                // If weight is provided overwrite the value.
                sarUsageEvent.Weight = usageEvent.Weight?? sarUsageEvent.Weight;
            }

            return sarUsageEvent;
        }

        private Recommendation ConvertToRecommendation(SarScoreResult sarScoreResult)
        {
            if (sarScoreResult.Recommended == 0 || sarScoreResult.Recommended > _itemIdIndex.Length)
            {
                return null;
            }

            return new Recommendation
            {
                RecommendedItemId = _itemIdIndex[sarScoreResult.Recommended - 1],
                Score = sarScoreResult.Score
            };
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
                    _scorer.Dispose();
                    _disposed = true;
                }
            }
        }

        private bool _disposed;
        private readonly ITracer _tracer;
        private readonly SarScorer _scorer;
        private readonly string[] _itemIdIndex;
        private readonly IDictionary<string, uint> _itemIdReverseLookup;
        private readonly ModelProperties _properties;
        private readonly UserHistoryStore _userHistoryStore;
    }
}