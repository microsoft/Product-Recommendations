// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.MachineLearning.EntryPoints;
using Newtonsoft.Json;
using Recommendations.Core.Evaluate;
using Recommendations.Core.Parsing;
using Recommendations.Core.Sar;

namespace Recommendations.Core.Train
{
    public class ModelTrainer
    {
        /// <summary>
        /// The maximal number of usage\catalog parsing errors that will be tolerated before failing
        /// </summary>
        public const int MaximumParsingErrorsCount = 100;

        /// <summary>
        /// Creates a new instance of the <see cref="ModelTrainer"/> class.
        /// </summary>
        /// <param name="tracer">A message tracer to use for logging</param>
        /// <param name="documentStore">A document store for storing user history is user-to-item is enabled</param>
        /// <param name="progressMessageReportDelegate">A delegate for reporting progress messages</param>
        public ModelTrainer(ITracer tracer = null, IDocumentStore documentStore = null, Action<string> progressMessageReportDelegate = null)
        {
            _tracer = tracer ?? new DefaultTracer();
            _progressMessageReportDelegate = progressMessageReportDelegate ?? (_ => { });

            if (documentStore != null)
            {
                _userHistoryStore = new UserHistoryStore(documentStore, _tracer, ReportUserHistoryProgress);
            }
        }

        /// <summary>
        /// Trains a model using the input files.
        /// </summary>
        /// <param name="settings">The trainer settings</param>
        /// <param name="usageFolderPath">The path to the folder of usage files</param>
        /// <param name="catalogFilePath">The path to the catalog file</param>
        /// <param name="evaluationFolderPath">The path to the evaluation file (optional) </param>
        /// <param name="cancellationToken">A cancellation token used to abort the training</param>
        public ModelTrainResult TrainModel(IModelTrainerSettings settings, string usageFolderPath, string catalogFilePath, string evaluationFolderPath, CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(usageFolderPath))
            {
                throw new ArgumentNullException(nameof(usageFolderPath));
            }

            if (!Directory.Exists(usageFolderPath))
            {
                throw new ArgumentException($"Directory '{usageFolderPath}' doesn't exists.", nameof(usageFolderPath));
            }

            if (settings.EnableColdItemPlacement)
            {
                if (string.IsNullOrWhiteSpace(catalogFilePath))
                {
                    throw new ArgumentNullException(nameof(catalogFilePath));
                }

                if (!File.Exists(catalogFilePath))
                {
                    throw new ArgumentException($"Catalog file '{catalogFilePath}' doesn't exists.", nameof(catalogFilePath));
                }
            }

            // create a temp work folder to intermediate files
            string workFolderPath = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
            _tracer.TraceVerbose($"Creating a temp work folder for storing intermediate files under '{workFolderPath}'");
            Directory.CreateDirectory(workFolderPath);

            var trainModelCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // train the model
                return TrainModelInternal(settings, workFolderPath, usageFolderPath, catalogFilePath,
                    evaluationFolderPath, trainModelCancellationTokenSource.Token);
            }
            catch (TaskCanceledException ex)
            {
                _tracer.TraceInformation($"Model training canceled. {ex}");
                throw;
            }
            catch (Exception ex)
            {
                // cancel any ongoing background tasks
                trainModelCancellationTokenSource.Cancel();

                var exception = new Exception("Exception while training model", ex);
                _tracer.TraceError(exception.ToString());
                throw exception;
            }
            finally
            {
                _tracer.TraceVerbose($"Deleting the temp work folder '{workFolderPath}'");
                Directory.Delete(workFolderPath, true);
            }
        }

        /// <summary>
        /// Trains a model using the input files
        /// </summary>
        /// <param name="settings">The trainer settings</param>
        /// <param name="workFolderPath">A temp work folder for storing intermediate files</param>
        /// <param name="usageFolderPath">The path to the folder of usage files</param>
        /// <param name="catalogFilePath">The path to the catalog file</param>
        /// <param name="evaluationFolderPath">The path to the evaluation file (optional) </param>
        /// <param name="cancellationToken">A cancellation token used to abort the training</param>
        private ModelTrainResult TrainModelInternal(IModelTrainerSettings settings, string workFolderPath, string usageFolderPath,
            string catalogFilePath, string evaluationFolderPath, CancellationToken cancellationToken)
        {
            var duration = ModelTraininigDuration.Start();
            var result = new ModelTrainResult {Duration = duration};
            
            var userIdsIndexMap = new ConcurrentDictionary<string, uint>();
            var itemIdsIndexMap = new ConcurrentDictionary<string, uint>();
            
            // parse the catalog file
            IList<SarCatalogItem> catalogItems = null;
            string[] catalogFeatureNames = null;
            if (!string.IsNullOrWhiteSpace(catalogFilePath) && File.Exists(catalogFilePath))
            {
                // report progress
                _progressMessageReportDelegate("Parsing Catalog File");

                // create a catalog file parser
                var catalogParser = new CatalogFileParser(MaximumParsingErrorsCount, itemIdsIndexMap, _tracer);

                // parse the catalog file
                result.CatalogFilesParsingReport = catalogParser.ParseCatalogFile(catalogFilePath, cancellationToken,
                    out catalogItems, out catalogFeatureNames);

                // record the catalog parsing duration
                duration.SetCatalogParsingDuration();
                _tracer.TraceInformation($"Catalog parsing completed in {duration.CatalogParsingDuration.TotalMinutes} minutes");
                
                // get the catalog items count
                result.CatalogItemsCount = catalogItems.Count;

                // fail the training if parsing had failed or yielded no items
                if (!result.CatalogFilesParsingReport.IsCompletedSuccessfuly || !catalogItems.Any())
                {
                    result.CompletionMessage = "Failed to parse catalog file or parsing found no valid items";
                    _tracer.TraceInformation(result.CompletionMessage);
                    return result;
                }

                // clear the catalog items list if it's not used anymore
                if (!settings.EnableColdItemPlacement)
                {
                    catalogItems.Clear();
                }
            }

            // report progress
            _progressMessageReportDelegate("Parsing Usage Events Files");

            // create a usage events files parser that skips events of unknown item ids (if catalog was provided))
            var usageEventsParser = new UsageEventsFilesParser(itemIdsIndexMap, userIdsIndexMap,
                MaximumParsingErrorsCount, catalogItems != null, _tracer);

            _tracer.TraceInformation("Parsing the usage event files");
            IList<SarUsageEvent> usageEvents;
            result.UsageFilesParsingReport =
                usageEventsParser.ParseUsageEventFiles(usageFolderPath, cancellationToken, out usageEvents);

            // record the usage files parsing duration
            duration.SetUsageFilesParsingDuration();
            _tracer.TraceInformation($"Usage file(s) parsing completed in {duration.UsageFilesParsingDuration.TotalMinutes} minutes");

            // fail the training if parsing had failed or yielded no events
            if (!result.UsageFilesParsingReport.IsCompletedSuccessfuly || !usageEvents.Any())
            {
                result.CompletionMessage = "Failed to parse usage file(s) or parsing found no valid items";
                _tracer.TraceInformation(result.CompletionMessage);
                return result;
            }

            _tracer.TraceInformation($"Found {userIdsIndexMap.Count} unique users");
            result.UniqueUsersCount = userIdsIndexMap.Count;

            _tracer.TraceInformation($"Found {itemIdsIndexMap.Count} unique items");
            result.UniqueItemsCount = usageEvents.Select(x=>x.ItemId).Distinct().Count();

            _tracer.TraceInformation("Extracting the indexed item ids from the item index map");
            string[] itemIdsIndex = itemIdsIndexMap.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();

            _tracer.TraceInformation($"Sorting the usage events based on the cooccurrenceUnit unit ({settings.CooccurrenceUnit})");
            switch (settings.CooccurrenceUnit)
            {
                case CooccurrenceUnit.User:
                    usageEvents = usageEvents.OrderBy(x => x.UserId).ToArray();
                    break;
                case CooccurrenceUnit.Timestamp:
                    usageEvents = usageEvents.OrderBy(x => x.Timestamp).ThenBy(x => x.UserId).ToArray();
                    break;
            }

            _tracer.TraceInformation("Finished sorting usage events.");

            Stopwatch storeUserHistoryDuration = null;
            Task storeUserHistoryTask = null;
            if (settings.EnableUserToItemRecommendations && _userHistoryStore != null)
            {
                storeUserHistoryDuration = Stopwatch.StartNew();
                _tracer.TraceInformation($"Extracting the indexed user ids from the user index map ({userIdsIndexMap.Count:N} users)");
                string[] userIdsIndex = userIdsIndexMap.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();

                _tracer.TraceInformation($"Asynchronously starting to store usage events per user (total of {usageEvents.Count:N} items)");
                storeUserHistoryTask = Task.Run(() =>
                    _userHistoryStore.StoreUserHistoryEventsAsync(usageEvents, userIdsIndex, cancellationToken), cancellationToken);
            }

            // if provided, parse the evaluation usage event files 
            int evaluationUsageEventsCount = 0;
            string parsedEvaluationUsageEventsFilePath = null;
            if (!string.IsNullOrWhiteSpace(evaluationFolderPath) && Directory.Exists(evaluationFolderPath))
            {
                // report progress
                _progressMessageReportDelegate("Parsing Evaluation Usage Events Files");

                _tracer.TraceInformation("Parsing the evaluation usage event files");
                IList<SarUsageEvent> evaluationUsageEvents;
                result.EvaluationFilesParsingReport = usageEventsParser.ParseUsageEventFiles(evaluationFolderPath,
                    cancellationToken, out evaluationUsageEvents);

                if (result.EvaluationFilesParsingReport.IsCompletedSuccessfuly)
                {
                    // set the evaluation usage events count
                    evaluationUsageEventsCount = evaluationUsageEvents.Count;

                    _tracer.TraceInformation("Storing the parsed usage events for evaluation to reduce memory print");
                    parsedEvaluationUsageEventsFilePath = Path.Combine(workFolderPath, Path.GetTempFileName());
                    File.WriteAllLines(parsedEvaluationUsageEventsFilePath,
                        evaluationUsageEvents.Select(JsonConvert.SerializeObject));
                }
                else
                {
                    _tracer.TraceWarning("Skipping model evaluation as it failed to parse evaluation usage files.");
                }

                // record the evaluation usage files parsing duration
                duration.SetEvaluationUsageFilesParsingDuration();
                _tracer.TraceInformation($"Evaluation usage file(s) parsing completed in {duration.EvaluationUsageFilesParsingDuration.TotalMinutes} minutes");
            }

            // clear the indices maps as they are no longer needed
            userIdsIndexMap.Clear();
            itemIdsIndexMap.Clear();

            cancellationToken.ThrowIfCancellationRequested();

            // report progress
            _progressMessageReportDelegate("Core Training");

            _tracer.TraceInformation("Training a new model using SAR trainer");
            IDictionary<string, double> catalogFeatureWeights;
            var sarTrainer = new SarTrainer(_tracer);
            IPredictorModel sarModel = sarTrainer.Train(settings, usageEvents, catalogItems, catalogFeatureNames, result.UniqueUsersCount,
                result.CatalogItemsCount ?? result.UniqueItemsCount, out catalogFeatureWeights, cancellationToken);

            _tracer.TraceInformation("SAR training was completed.");

            // create the trained model properties
            var modelProperties = new ModelProperties
            {
                IncludeHistory = settings.AllowSeedItemsInRecommendations,
                EnableUserAffinity = settings.EnableUserAffinity,
                IsUserToItemRecommendationsSupported = settings.EnableUserToItemRecommendations,
                Decay = TimeSpan.FromDays(settings.DecayPeriodInDays),
                ReferenceDate = usageEventsParser.MostRecentEventTimestamp,
                UniqueUsersCount = result.UniqueUsersCount,
            };
            
            // create the trained model
            result.Model = new TrainedModel(sarModel, modelProperties, itemIdsIndex);

            // set the catalog features weights
            result.CatalogFeatureWeights = catalogFeatureWeights;

            // record the core training duration
            duration.SetTrainingDuration();

            // run model evaluation if evaluation usage event are available
            if (evaluationUsageEventsCount > 0 && parsedEvaluationUsageEventsFilePath != null)
            {
                // report progress
                _progressMessageReportDelegate("Evaluating Trained Model");

                var evaluationUsageEvents = new List<SarUsageEvent>(evaluationUsageEventsCount);
                
                // load the evaluation usage events
                using (var reader = new StreamReader(parsedEvaluationUsageEventsFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        evaluationUsageEvents.Add(JsonConvert.DeserializeObject<SarUsageEvent>(reader.ReadLine()));
                    }
                }
                    
                _tracer.TraceInformation("Starting model evaluation");
                var evaluator = new ModelEvaluator(_tracer);
                result.ModelMetrics = evaluator.Evaluate(result.Model, usageEvents, evaluationUsageEvents, cancellationToken);

                // record the evaluation duration
                duration.SetEvaluationDuration();
            }

            if (storeUserHistoryTask != null)
            {
                _tracer.TraceInformation("Waiting for storing of usage events per user (user history) to complete");

                if (!storeUserHistoryTask.IsCompleted)
                {
                    _progressMessageReportDelegate("Storing User History");

                    // set the reporting flag to true so usage history upload progress will get reported to model status 
                    _reportUserHistoryProgress = true;
                }

                try
                {
                    storeUserHistoryTask.Wait(cancellationToken);
                    storeUserHistoryDuration?.Stop();
                    duration.StoringUserHistoryDuration = storeUserHistoryDuration?.Elapsed;
                    _tracer.TraceInformation(
                        $"Storing usage events per user (user history) to complete after {duration.StoringUserHistoryDuration.Value.TotalMinutes} minutes");
                }
                catch (AggregateException ex)
                {
                    var exception = new Exception("Exception while trying to store user history", ex);
                    _tracer.TraceError(exception.ToString());
                    throw exception;
                }
            }

            // stop measuring the duration and record the total duration
            duration.Stop();

            // return the train result
            result.CompletionMessage = "Model Training Completed Successfully";
            return result;
        }
        
        private void ReportUserHistoryProgress(string progressMessage)
        {
            _tracer.TraceInformation(progressMessage);
            if (_reportUserHistoryProgress)
            {
                _progressMessageReportDelegate(progressMessage);
            }
        }
        
        private bool _reportUserHistoryProgress;
        private readonly ITracer _tracer;
        private readonly UserHistoryStore _userHistoryStore;
        private readonly Action<string> _progressMessageReportDelegate;
    }
}
