// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Recommendations.Common;
using Recommendations.Common.Api;
using Recommendations.Core;
using Recommendations.Core.Parsing;
using Recommendations.Core.Train;

[assembly: InternalsVisibleTo("Recommendations.UnitTest")]

namespace Recommendations.WebJob
{
    internal class WebJobLogic
    {
        /// <summary>
        /// Creates a new instance of the <see cref="WebJobLogic"/> class.
        /// </summary>
        /// <param name="modelsProvider">A models provider</param>
        /// <param name="modelsRegistry">A model registry</param>
        public WebJobLogic(ModelsProvider modelsProvider, ModelsRegistry modelsRegistry)
        {
            if (modelsProvider == null)
            {
                throw new ArgumentNullException(nameof(modelsProvider));
            }

            if (modelsRegistry == null)
            {
                throw new ArgumentNullException(nameof(modelsRegistry));
            }

            _modelsProvider = modelsProvider;
            _modelsRegistry = modelsRegistry;
        }

        /// <summary>
        /// Starts model training
        /// </summary>
        /// <param name="modelId">The id of the model to train</param>
        /// <param name="cancellationToken">A cancellation token used to abort the operation</param>
        public async Task TrainModelAsync(Guid modelId, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("Getting model from model registry");
            Model model = await _modelsRegistry.GetModelAsync(modelId, cancellationToken);
            if (model == null)
            {
                Trace.TraceInformation("Model doesn't exist (deleted?) - skipping build model");
                return;
            }

            // check if model is ready for build 
            if (model.Status == ModelStatus.Completed || model.Status == ModelStatus.Failed)
            {
                Trace.TraceInformation($"Skipping model build. Model Status : {model.Status}");
                return;
            }

            // updated the model status to 'in progress'
            Trace.TraceInformation($"Updating model status to {ModelStatus.InProgress}");
            await _modelsRegistry.UpdateModelAsync(modelId, cancellationToken,
                ModelStatus.InProgress, "Starting Model Training");

            // create a new cancellation token source that is linked to the provided cancellation token
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // create timer to periodically poll table to see if the model was deleted
            Trace.TraceVerbose($"Starting monitoring model '{modelId}' status to cancel the training if model is deleted");
            using (StartModelStatusMonitor(modelId, cancellationTokenSource))
            {
                Trace.TraceInformation("Starting model training");
                if (await TrainModelAsync(modelId, model.Parameters, cancellationTokenSource.Token))
                {
                    Trace.TraceInformation("Model training completed successfully.");

                    // if a default model is not defined, set the newly built model as the default
                    await TrySettingDefaultModelIfEmpty(modelId, cancellationTokenSource.Token);
                }
            }

            Trace.TraceInformation("Model training completed successfully");
        }

        /// <summary>
        /// Creates a timer that periodically checks if a model was deleted from the registry and cancel 
        /// the on going model training.
        /// </summary>
        private Timer StartModelStatusMonitor(Guid modelId, CancellationTokenSource trainingCancellationTokenSource)
        {
            // define a callback that checks the model status for cancellation 
            TimerCallback callback = _ =>
            {
                try
                {
                    Trace.TraceVerbose($"Model Status Monitor: Trying to get the model '{modelId}' from the registry");
                    Model model = _modelsRegistry.GetModel(modelId);

                    // if the model was deleted, cancel the model training
                    if (model == null)
                    {
                        Trace.TraceInformation($"Model Status Monitor: Model '{modelId}' was deleted from the registry - aborting the model training");
                        trainingCancellationTokenSource.Cancel();
                    }
                }
                catch (Exception ex)
                {
                    // log and ignore error
                    Trace.TraceWarning($"Model Status Monitor: Exception while trying to get model status. Exception: {ex}");
                }
            };

            // create and return a timer
            return new Timer(callback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(3));
        }

        /// <summary>
        /// Starts model training
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> TrainModelAsync(Guid modelId, ModelTrainingParameters parameters,
            CancellationToken cancellationToken)
        {
            // create an auto reset event to ensure that model status updates happen one at a time
            var ongoingUpdateEvent = new AutoResetEvent(true);

            // create a progress messages event handler for updating the model status message in the registry
            Action<string> progressMessageHandler = progressMessage => ModelTrainingProgressMessagesEventHandler(
                progressMessage, modelId, ongoingUpdateEvent, cancellationToken);
            
            Trace.TraceInformation($"Starting model '{modelId}' training");
            ModelTrainResult result = await _modelsProvider.TrainAsync(
                modelId, parameters, progressMessageHandler, cancellationToken);

            // get the model status
            ModelStatus newModelStatus = result.IsCompletedSuccessfuly ? ModelStatus.Completed : ModelStatus.Failed;
            Trace.TraceInformation($"Model training completed with status '{newModelStatus}'");

            Trace.TraceInformation("Extracting model statistics from the model training result");
            ModelStatistics modelStatistics = CreateModelStatistics(result, parameters);

            Trace.TraceInformation("Wait for any ongoing model status message updates before updating the final status");
            ongoingUpdateEvent.WaitOne();

            Trace.TraceInformation("Update the model status and statistics to the registry");
            await _modelsRegistry.UpdateModelAsync(modelId, cancellationToken,
                newModelStatus, result.CompletionMessage, modelStatistics);

            return result.IsCompletedSuccessfuly;
        }

        /// <summary>
        /// A progress messages event handler that updates a model status messages in the registry
        /// </summary>
        /// <param name="progressMessage">The progress message to update</param>
        /// <param name="modelId">The model id in context</param>
        /// <param name="ongoingUpdateEvent">An auto reset event that ensures updates happen one at a time</param>
        /// <param name="cancellationToken">The cancellation token assigned to the operation.</param>
        private async void ModelTrainingProgressMessagesEventHandler(string progressMessage, Guid modelId, AutoResetEvent ongoingUpdateEvent, CancellationToken cancellationToken)
        {
            try
            {
                Trace.TraceVerbose(
                    "Waiting for any previous model status message update to complete (if still ongoing)");
               ongoingUpdateEvent.WaitOne();

                Trace.TraceVerbose($"Updating the model '{modelId}' status message to '{progressMessage}'");
                await _modelsRegistry.UpdateModelAsync(modelId, cancellationToken, statusMessage: progressMessage);
            }
            catch (Exception e)
            {
                // ignore exceptions thrown while trying to update status message
                Trace.TraceInformation(
                    $"Failed updating model status message to '{progressMessage}', skipping. Exception: {e}");
            }
            finally
            {
                // set the event to release any waiting threads 
                ongoingUpdateEvent.Set();
            }
        }

        /// <summary>
        /// If no default model is set, tries to set the model as the default model.
        /// </summary>
        private async Task TrySettingDefaultModelIfEmpty(Guid modelId, CancellationToken cancellationToken)
        {
            try
            {
                var defaultModelId = await _modelsRegistry.GetDefaultModelIdAsync(cancellationToken);
                if (defaultModelId == null)
                {
                    Trace.TraceInformation($"Default model is not defined - setting model '{modelId}' as the default model");
                    await _modelsRegistry.SetDefaultModelIdAsync(modelId, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceWarning($"Skipping setting model as the default model (if non defined) due to exception: {exception}");
            }
        }

        /// <summary>
        /// Create model statistics out of the model training result
        /// </summary>
        private static ModelStatistics CreateModelStatistics(ModelTrainResult result, ModelTrainingParameters parameters)
        {
            var statistics = new ModelStatistics
            {
                // set the total duration
                TotalDuration = result.Duration.TotalDuration,

                // set the core training duration
                TrainingDuration = result.Duration.TrainingDuration,

                // set the storing user history duration
                StoringUserHistoryDuration = result.Duration.StoringUserHistoryDuration,

                // create the catalog parsing report
                CatalogParsingReport = CreateParsingReport(result.CatalogFilesParsingReport,
                    result.Duration.CatalogParsingDuration,
                    string.IsNullOrWhiteSpace(parameters.CatalogFileRelativePath)
                        ? null
                        : Path.GetDirectoryName(parameters.CatalogFileRelativePath)),

                // create the usage files parsing report
                UsageEventsParsingReport = CreateParsingReport(result.UsageFilesParsingReport,
                    result.Duration.UsageFilesParsingDuration,
                    parameters.UsageRelativePath),

                // set the number of items in catalog
                NumberOfCatalogItems = result.CatalogItemsCount,

                // set the number of valid items in usage files
                NumberOfUsageItems = result.UniqueItemsCount,

                // set the number of unique users in usage files
                NumberOfUsers = result.UniqueUsersCount,

                // set the catalog coverage when applicable
                CatalogCoverage =
                    result.CatalogItemsCount != null &&
                    result.CatalogItemsCount != 0
                        ? (double)result.UniqueItemsCount / result.CatalogItemsCount
                        : null,

                // set the catalog features weights, if calculated
                CatalogFeatureWeights = result.CatalogFeatureWeights?.Count > 0 ? result.CatalogFeatureWeights : null
            };

            // set the evaluation statistics if available 
            if (!string.IsNullOrWhiteSpace(parameters.EvaluationUsageRelativePath))
            {
                // create evaluation result
                statistics.EvaluationResult = new ModelEvaluationResult
                {
                    // set the evaluation duration 
                    Duration = result.Duration.EvaluationDuration,

                    // set the evaluation result
                    Metrics = result.ModelMetrics,

                    // create the evaluation usage files parsing report
                    EvaluationUsageEventsParsingReport =
                        CreateParsingReport(result.EvaluationFilesParsingReport,
                            result.Duration.EvaluationUsageFilesParsingDuration,
                            parameters.EvaluationUsageRelativePath)
                };
            }

            return statistics;
        }

        /// <summary>
        /// Creates <see cref="ParsingReport"/> from a given <see cref="FileParsingReport"/> and other parameters
        /// </summary>
        internal static ParsingReport CreateParsingReport(FileParsingReport report, TimeSpan duration, string fileRootPath)
        {
            if (report == null)
            {
                return null;
            }

            var parsingReport = new ParsingReport
            {
                Duration = duration,
                TotalLinesCount = report.TotalLinesCount,
                SuccessfulLinesCount = report.SuccessfulLinesCount
            };

            if (report.HasErrors)
            {
                parsingReport.Errors = parsingReport.Errors ?? new List<LineParsingError>();

                parsingReport.Errors.AddRange(
                    GetParsingErrors(report.Errors, fileRootPath));
            }

            if (report.HasWarnings)
            {
                parsingReport.Errors = parsingReport.Errors ?? new List<LineParsingError>();
                parsingReport.Errors.AddRange(
                    GetParsingErrors(report.Warnings, fileRootPath));
            }

            return parsingReport;
        }

        /// <summary>
        /// Extracts list of <see cref="LineParsingError"/> from a given list of <see cref="ParsingError"/>
        /// </summary>
        private static List<LineParsingError> GetParsingErrors(List<ParsingError> errors, string fileRootPath)
        {
            return errors.Where(err => err != null)
                .GroupBy(err => err.ErrorReason)
                .Select(errGrp => new {sample = errGrp.First(), count = errGrp.Count()})
                .Select(error =>
                    new LineParsingError
                    {
                        Sample = new ParsingErrorSample
                        {
                            FileRelativePath = $"{fileRootPath}/{error.sample.FileName}",
                            LineNumber = error.sample.LineNumber
                        },
                        Error = error.sample.ErrorReason,
                        Count = error.count
                    })
                .ToList();
        }

        private readonly ModelsProvider _modelsProvider;
        private readonly ModelsRegistry _modelsRegistry;
        private static readonly ITracer Trace = new Tracer(nameof(WebJobLogic));
    }
}
