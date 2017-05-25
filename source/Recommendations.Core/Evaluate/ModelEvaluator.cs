// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.MachineLearning;
using Microsoft.MachineLearning.Api;
using Microsoft.MachineLearning.Data;
using Microsoft.MachineLearning.Recommend;
using Recommendations.Core.Recommend;
using Recommendations.Core.Sar;

namespace Recommendations.Core.Evaluate
{
    /// <summary>
    /// A class for evaluation a trained model by using a test data set to compute precision and diversity metrics
    /// </summary>
    internal class ModelEvaluator
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ModelEvaluator"/> class.
        /// </summary>
        /// <param name="tracer">A message tracer to use for logging</param>
        public ModelEvaluator(ITracer tracer)
        {
            _tracer = tracer ?? new DefaultTracer();
        }

        /// <summary>
        /// Computes Precision and Diversity metrics for given model, usage events it was trained on, and evaluation events it 
        /// would be evaluated on.
        /// </summary>
        /// <param name="model">Model to be used for scoring</param>
        /// <param name="usageEvents">List of usage events</param>
        /// <param name="evaluationUsageEvents">List of evaluation events</param>
        /// <param name="cancellationToken">A cancellation token used to abort the evaluation</param>
        /// <returns></returns>
        public ModelMetrics Evaluate(ITrainedModel model, IList<SarUsageEvent> usageEvents,
            IList<SarUsageEvent> evaluationUsageEvents, CancellationToken cancellationToken)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (usageEvents == null)
            {
                throw new ArgumentNullException(nameof(usageEvents));
            }

            if (evaluationUsageEvents == null)
            {
                throw new ArgumentNullException(nameof(evaluationUsageEvents));
            }

            // score the test usage events
            IList<SarScoreResult> scores = ScoreTestUsers(model, usageEvents, evaluationUsageEvents);

            // use the scoring result to compute precision and diversity
            return ComputeMetrics(usageEvents, evaluationUsageEvents, scores, cancellationToken);
        }

        /// <summary>
        /// Scores the users in <see cref="usageEvents"/> who are also present in <see cref="evaluationUsageEvents"/>
        /// </summary>
        /// <param name="model">Model to be used for scoring</param>
        /// <param name="usageEvents">List of usage events</param>
        /// <param name="evaluationUsageEvents">List of evaluation events</param>
        /// <returns></returns>
        private IList<SarScoreResult> ScoreTestUsers(ITrainedModel model, IList<SarUsageEvent> usageEvents,
            IList<SarUsageEvent> evaluationUsageEvents)
        {
            // Score the test users
            var evaluationRecommender = new Recommender(model, null, _tracer);

            // extract the user ids from the evaluation usage events
            HashSet<uint> evaluationUsers = new HashSet<uint>(evaluationUsageEvents.Select(usageEvent => usageEvent.UserId));

            // filter out usage events of users not found in the evaluation set
            IEnumerable<SarUsageEvent> usageEventsFiltered =
                usageEvents.Where(usageEvent => evaluationUsers.Contains(usageEvent.UserId));

            // group usage event by user, and calculate score
            List<SarScoreResult> scores =
                usageEventsFiltered.GroupBy(usageEvent => usageEvent.UserId)
                    .SelectMany(group => evaluationRecommender.ScoreUsageEvents(group.ToList(), RecommendationCount))
                    .ToList();
            
            return scores;
        }

        private ModelMetrics ComputeMetrics(
            IList<SarUsageEvent> usageEvents,
            IList<SarUsageEvent> evaluationEvents,
            IList<SarScoreResult> scores,
            CancellationToken cancellationToken)
        {
            if (!scores.Any() || !usageEvents.Any() || !evaluationEvents.Any())
            {
                _tracer.TraceWarning(
                    $"Operation '{nameof(ComputeMetrics)}' returning empty results. Scores: '{scores.Count}', Usage Events: '{usageEvents.Count}', Evaluation Events: '{evaluationEvents.Count}'");
                return new ModelMetrics();
            }

            // convert the usage items to the evaluation format
            List<SarEvaluationUsageEvent> usageEventsFormatted = usageEvents.Select(ToEvaluationUsageEvent).ToList();

            // convert the evaluation usage items to the evaluation format
            List<SarEvaluationUsageEvent> evaluationEventsFormatted = 
                evaluationEvents.Select(ToEvaluationUsageEvent).ToList();

            // convert the scores items to the evaluation format
            List<SarEvaluationUsageEvent> scoresFormatted = scores.Select(ToEvaluationUsageEvent).ToList();

            using (TlcEnvironment environment = new TlcEnvironment())
            {
                environment.AddListener<ChannelMessage>(_tracer.TraceChannelMessage);

                // Create a precision evaluator.
                PrecisionAtKEvaluator precisionEvaluator = new PrecisionAtKEvaluator(
                    environment,
                    new PrecisionAtKEvaluator.Arguments
                    {
                        k = MaxPrecisionK
                    },
                    environment.CreateStreamingDataView(scoresFormatted),
                    environment.CreateStreamingDataView(evaluationEventsFormatted));
                cancellationToken.ThrowIfCancellationRequested();

                // Create a diversity evaluator.
                DiversityAtKEvaluator diversityEvaluator = new DiversityAtKEvaluator(
                    environment,
                    new DiversityAtKEvaluator.Arguments
                    {
                        buckets = DiversityBuckets
                    },
                    environment.CreateStreamingDataView(scoresFormatted),
                    environment.CreateStreamingDataView(usageEventsFormatted));
                cancellationToken.ThrowIfCancellationRequested();

                // Compute Precision metrics
                IList<PrecisionAtKEvaluator.MetricItem> precisionMetrics =
                    precisionEvaluator.Evaluate()
                        .AsEnumerable<PrecisionAtKEvaluator.MetricItem>(environment, false)
                        .ToList();
                var modelPrecisionMetrics = precisionMetrics.Select(
                    metric => new PrecisionMetric
                    {
                        K = (int) metric.K,
                        Percentage = Math.Round(metric.PrecisionAtK*100, 3),
                        UsersInTest = (int?) metric.TotalUsers
                    }).ToList();
                    
                cancellationToken.ThrowIfCancellationRequested();

                // Compute Diversity metrics
                IList<DiversityAtKEvaluator.MetricItem> diversityMetrics =
                    diversityEvaluator.Evaluate()
                        .AsEnumerable<DiversityAtKEvaluator.MetricItem>(environment, false)
                        .ToList();

                ModelDiversityMetrics modelDiversityMetrics =
                    new ModelDiversityMetrics
                    {
                        PercentileBuckets = diversityMetrics.Select(bucket => new PercentileBucket
                        {
                            Min = (int) bucket.BucketMin,
                            Max = (bool) (bucket.BucketLim == 101) ? 100 : (int) bucket.BucketLim,
                            Percentage = Math.Round(bucket.RecommendedItemsFraction*100, 3)
                        }).ToList(),

                        UniqueItemsRecommended = (int?) diversityMetrics.First().DistinctRecommendations,
                        TotalItemsRecommended = (int?) diversityMetrics.First().TotalRecommendations,
                        UniqueItemsInTrainSet = (int?) diversityMetrics.First().TotalItemsEvaluated
                    };

                return new ModelMetrics
                {
                    ModelPrecisionMetrics = modelPrecisionMetrics,
                    ModelDiversityMetrics = modelDiversityMetrics
                };
            }
        }

        /// <summary>
        /// Converts the input <see cref="SarUsageEvent"/> instance to a <see cref="SarEvaluationUsageEvent"/>
        /// </summary>
        private static SarEvaluationUsageEvent ToEvaluationUsageEvent(SarUsageEvent usageEvent)
        {
            return new SarEvaluationUsageEvent
            {
                UserId = usageEvent.UserId.ToString(),
                ItemId = usageEvent.ItemId.ToString()
            };
        }

        /// <summary>
        /// Converts the input <see cref="SarScoreResult"/> instance to a <see cref="SarEvaluationUsageEvent"/>
        /// </summary>
        private static SarEvaluationUsageEvent ToEvaluationUsageEvent(SarScoreResult scoreResult)
        {
            return new SarEvaluationUsageEvent
            {
                UserId = scoreResult.User.ToString(),
                ItemId = scoreResult.Recommended.ToString()
            };
        }

        // Ending of the bucket is exclusive. Therefore instead of 100, we need to pass in 101.
        private const string DiversityBuckets = "0-90,90-99,99-101";
        private const int RecommendationCount = 5;
        private const int MaxPrecisionK = 5;

        private readonly ITracer _tracer;
    }
}
