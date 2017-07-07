// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Recommendations.Common.Api;
using Recommendations.Common.Cloud;
using Recommendations.Core;
using Recommendations.Core.Recommend;
using Recommendations.Core.Train;

namespace Recommendations.Common
{
    /// <summary>
    /// Model provider
    /// </summary>
    public class ModelsProvider : IDisposable
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ModelsProvider"/> class.
        /// </summary>
        /// <param name="blobContainerProvider">A blob container provider</param>
        /// <param name="documentStoreProvider">A user history document store provider</param>
        /// <param name="trainedModelsLocalRootPath">A local folder to store trained model files</param>
        internal ModelsProvider(IBlobContainerProvider blobContainerProvider,
            IDocumentStoreProvider documentStoreProvider,
            string trainedModelsLocalRootPath)
        {
            if (blobContainerProvider == null)
            {
                throw new ArgumentNullException(nameof(blobContainerProvider));
            }

            if (documentStoreProvider == null)
            {
                throw new ArgumentNullException(nameof(documentStoreProvider));
            }

            if (string.IsNullOrWhiteSpace(trainedModelsLocalRootPath))
            {
                throw new ArgumentNullException(nameof(trainedModelsLocalRootPath));
            }

            _blobContainerProvider = blobContainerProvider;

            _documentStoreProvider = documentStoreProvider;

            // get the models container that stores trained models
            _modelsContainer = _blobContainerProvider.GetBlobContainer(ModelsBlobContainerName, true);

            // create the local directory if not exists
            _trainedModelsLocalRootPath = trainedModelsLocalRootPath;
            Directory.CreateDirectory(_trainedModelsLocalRootPath);
        }

        /// <summary>
        /// Trains new model
        /// </summary>
        /// <param name="modelId">Model ID of the model to create</param>
        /// <param name="trainingParameters">Parameters of the new model to train</param>
        /// <param name="progressMessageReportDelegate">A delegate for handling progress messages</param>
        /// <param name="cancellationToken">A cancellation token used to abort the operation</param>
        /// <returns>The model training result</returns>
        public async Task<ModelTrainResult> TrainAsync(Guid modelId, ModelTrainingParameters trainingParameters, 
            Action<string> progressMessageReportDelegate, CancellationToken cancellationToken)
        {
            Trace.TraceVerbose($"Model training started for model with id '{modelId}'.");
            progressMessageReportDelegate = progressMessageReportDelegate ?? (_ => { });

            // create a temporary local folder for the model training files
            string trainingTempPath = Path.Combine(_trainedModelsLocalRootPath,
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(trainingTempPath);
            IDocumentStore modelDocumentStore = null;

            try
            {
                // report progress
                progressMessageReportDelegate("Downloading Training blobs");

                // download the training files
                TrainingLocalFilePaths localFilePaths =
                    await DownloadTrainingBlobsAsync(modelId, trainingTempPath, trainingParameters, cancellationToken);

                // check if the operation was cancelled
                cancellationToken.ThrowIfCancellationRequested();

                // create user history store if user-to-item is enabled 
                if (trainingParameters.EnableUserToItemRecommendations)
                {
                    Trace.TraceInformation($"Creating user history document store for model '{modelId}'");
                    modelDocumentStore = _documentStoreProvider.GetDocumentStore(modelId);
                    modelDocumentStore.CreateIfNotExists();
                }

                // create a model trainer
                var modelTrainer = new ModelTrainer(new Tracer(nameof(ModelTrainer)), modelDocumentStore,
                    progressMessageReportDelegate);

                // train the model
                ModelTrainResult result = TrainModel(modelTrainer, modelId, trainingParameters,
                    localFilePaths, cancellationToken);
                if (!result.IsCompletedSuccessfuly)
                {
                    Trace.TraceWarning($"Model training failed for model with id '{modelId}'.");
                    return result;
                }

                // serialize and upload the trained model
                using (Stream modelStream = new MemoryStream())
                {
                    Trace.TraceInformation("Serializing the trained model to a stream");
                    SerializeTrainedModel(result.Model, modelStream, modelId);

                    // rewind the stream before reading
                    modelStream.Seek(0, SeekOrigin.Begin);

                    // upload the serialized model to blob storage
                    await UploadTrainedModelAsync(modelStream, modelId, cancellationToken);
                }

                // return the result
                Trace.TraceInformation($"Model training completed for model with id '{modelId}'. Result: {result}");
                return result;
            }
            finally
            {
                Trace.TraceInformation($"Deleting the training temporary local folder '{trainingTempPath}'.");
                Directory.Delete(trainingTempPath, true);
            }
        }

        /// <summary>
        /// Get recommendations from a model
        /// </summary>
        /// <param name="modelId">The id of the model to recommend with</param>
        /// <param name="usageEvents">Usage events to get recommendations for</param>
        /// <param name="userId">An optional id of the user to provide recommendations for. Any stored usage events associated with this user will be considered when getting recommendations</param>
        /// <param name="recommendationCount">Number of recommendations to get</param>
        /// <param name="cancellationToken">A cancellation token used to abort the operation</param>
        public async Task<IList<Recommendation>> ScoreAsync(Guid modelId, IEnumerable<IUsageEvent> usageEvents, string userId, int recommendationCount, CancellationToken cancellationToken)
        {
            // return empty result if no recommendations requested 
            if (recommendationCount <= 0)
            {
                Trace.TraceVerbose($"Requested '{recommendationCount}' recommendation for model '{modelId}' - returning empty recommendations.");
                return new Recommendation[0];
            }

            Trace.TraceInformation($"Getting or creating a recommender for model '{modelId}'");
            Recommender recommender = await GetOrCreateRecommenderAsync(modelId, cancellationToken);

            try
            {
                Trace.TraceInformation($"Getting recommendations for model '{modelId}'");
                return recommender.GetRecommendations(usageEvents, userId, recommendationCount);
            }
            catch (Exception ex)
            {
                var exception = new Exception(
                    $"Exception while trying to get recommendations for model with id {modelId}", ex);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Delete a trained model resource.
        /// </summary>
        /// <param name="modelId">The id of the model to delete</param>
        /// <param name="cancellationToken">A cancellation token used to abort the operation</param>
        public async Task DeleteModelAsync(Guid modelId, CancellationToken cancellationToken)
        {
            try
            {
                // get the trained model blob name
                string modelBlobName = GetModelBlobName(modelId);

                Trace.TraceInformation($"Deleting model blob '{modelBlobName}'");

                // delete remote model directory
                await _modelsContainer.DeleteBlobIfExistsAsync(modelBlobName, cancellationToken);

                Trace.TraceInformation($"Deleting user history store of model '{modelId}'");
                IDocumentStore modelDocumentStore = _documentStoreProvider.GetDocumentStore(modelId);
                await modelDocumentStore.DeleteIfExistsAsync();
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Failed to delete trained model blob '{modelId}'", storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        #region Private Helper Members
        
        /// <summary>
        /// Downloads the catalog, usage event files and evaluation usage files to local disk.
        /// </summary>
        private async Task<TrainingLocalFilePaths> DownloadTrainingBlobsAsync(Guid modelId, string localRootPath, 
            ModelTrainingParameters trainingParameters, CancellationToken cancellationToken)
        {
            try
            {
                var trainingFiles = new TrainingLocalFilePaths
                {
                    // set local usage directory name
                    UsageFolderPath = Path.Combine(localRootPath, UsageDirectoryName)
                };

                // create the local folder for usage events files
                Directory.CreateDirectory(trainingFiles.UsageFolderPath);

                // get the root blob container of the catalog and usage files
                IBlobContainer trainingBlobsContainer =
                    _blobContainerProvider.GetBlobContainer(trainingParameters.BlobContainerName);

                // check if the provided path represents a single file
                if (await trainingBlobsContainer.ExistsAsync(trainingParameters.UsageRelativePath, cancellationToken))
                {
                    string usageEventsBlobName = trainingParameters.UsageRelativePath;

                    // set local usage events file path
                    string usageEventFileName = Path.GetFileName(usageEventsBlobName) ?? string.Empty;
                    string usageFilePath = Path.Combine(trainingFiles.UsageFolderPath, usageEventFileName);

                    Trace.TraceInformation($"Downloading usage events blob '{usageEventsBlobName}'");
                    await trainingBlobsContainer.DownloadBlobAsync(usageEventsBlobName, usageFilePath,
                        cancellationToken);
                }
                else
                {
                    Trace.TraceInformation(
                        $"Listing all the usage events blobs under '{trainingParameters.UsageRelativePath}'");
                    IList<string> usageEventsBlobNames = await trainingBlobsContainer.ListBlobsAsync(
                        trainingParameters.UsageRelativePath, cancellationToken);

                    Trace.TraceInformation(
                        $"Downloading all the usage events blobs (Found {usageEventsBlobNames.Count})");
                    foreach (string usageEventsBlobName in usageEventsBlobNames)
                    {
                        // set local usage events file path
                        string usageEventFileName = Path.GetFileName(usageEventsBlobName) ?? string.Empty;
                        string usageFilePath = Path.Combine(trainingFiles.UsageFolderPath, usageEventFileName);

                        Trace.TraceInformation($"Downloading usage events blob '{usageEventsBlobName}'");
                        await trainingBlobsContainer.DownloadBlobAsync(usageEventsBlobName, usageFilePath,
                            cancellationToken);
                    }
                }

                // download the catalog file, if required and provided
                if (trainingParameters.EnableColdItemPlacement && !string.IsNullOrWhiteSpace(trainingParameters.CatalogFileRelativePath))
                {
                    // set local catalog file path
                    var catalogFileName = Path.GetFileName(trainingParameters.CatalogFileRelativePath);
                    trainingFiles.CatalogFilePath = Path.Combine(localRootPath, catalogFileName);

                    Trace.TraceInformation($"Downloading catalog blob '{trainingFiles.CatalogFilePath}'");
                    await trainingBlobsContainer.DownloadBlobAsync(trainingParameters.CatalogFileRelativePath,
                        trainingFiles.CatalogFilePath, cancellationToken);
                }

                // download the evaluation files if provided
                if (!string.IsNullOrWhiteSpace(trainingParameters.EvaluationUsageRelativePath))
                {
                    // set local evaluation folder
                    trainingFiles.EvaluationUsageFolderPath = Path.Combine(localRootPath, EvaluationUsageLocalDirectoryName);
                    
                    // create the local folder for evaluation usage events files
                    Directory.CreateDirectory(trainingFiles.EvaluationUsageFolderPath);

                    // check if the provided path represents a single file
                    if (await trainingBlobsContainer.ExistsAsync(trainingParameters.EvaluationUsageRelativePath,
                        cancellationToken))
                    {
                        string usageEventsBlobName = trainingParameters.EvaluationUsageRelativePath;

                        // set local usage events file path
                        string usageEventFileName = Path.GetFileName(usageEventsBlobName) ?? string.Empty;
                        string usageFilePath =
                            Path.Combine(trainingFiles.EvaluationUsageFolderPath, usageEventFileName);

                        Trace.TraceInformation($"Downloading evaluation usage events blob '{usageEventsBlobName}'");
                        await trainingBlobsContainer.DownloadBlobAsync(usageEventsBlobName, usageFilePath,
                            cancellationToken);
                    }
                    else
                    {
                        Trace.TraceInformation(
                            $"Listing all the evaluation usage events blobs under '{trainingParameters.EvaluationUsageRelativePath}'");
                        IList<string> evaluationUsageEventsBlobNames = await trainingBlobsContainer.ListBlobsAsync(
                            trainingParameters.EvaluationUsageRelativePath, cancellationToken);

                        Trace.TraceInformation(
                            $"Downloading all the evaluation usage events blobs (Found {evaluationUsageEventsBlobNames.Count})");
                        foreach (string usageEventsBlobName in evaluationUsageEventsBlobNames)
                        {
                            // set local usage events file path
                            string usageEventFileName = Path.GetFileName(usageEventsBlobName) ?? string.Empty;
                            string usageFilePath =
                                Path.Combine(trainingFiles.EvaluationUsageFolderPath, usageEventFileName);

                            Trace.TraceInformation($"Downloading evaluation usage events blob '{usageEventsBlobName}'");
                            await trainingBlobsContainer.DownloadBlobAsync(usageEventsBlobName, usageFilePath,
                                cancellationToken);
                        }
                    }
                }

                return trainingFiles;
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Failed downloading training files from storage. Model id: {modelId}",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }
        
        /// <summary>
        /// Trains a model using the local training files
        /// </summary>
        private ModelTrainResult TrainModel(ModelTrainer modelTrainer, Guid modelId, ModelTrainingParameters trainingParameters, 
            TrainingLocalFilePaths localFilePaths, CancellationToken cancellationToken)
        {
            try
            {
                Trace.TraceInformation($"Training model '{modelId}' using the local training files as input");
                return modelTrainer.TrainModel(trainingParameters, localFilePaths.UsageFolderPath,
                    localFilePaths.CatalogFilePath, localFilePaths.EvaluationUsageFolderPath, cancellationToken);
            }
            catch (Exception ex)
            {
                var exception = new Exception($"Exception while trying to train model with id: {modelId}", ex);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Serializes the input trained model into a zip stream
        /// </summary>
        private void SerializeTrainedModel(ITrainedModel trainedModel, Stream targetStream, Guid modelId)
        {
            try
            {
                // create a zip stream, leaving the target stream open after disposing
                using (var gzipStream = new GZipStream(targetStream, CompressionMode.Compress, leaveOpen: true))
                {
                    // binary serialize the trained model to the stream
                    new BinaryFormatter().Serialize(gzipStream, trainedModel);
                }
            }
            catch (Exception ex)
            {
                var exception = new Exception($"Failed serializing trained model {modelId} to stream", ex);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Deserialize the input stream as a trained model
        /// </summary>
        private ITrainedModel DeserializeTrainedModel(Stream trainedModelStream, Guid modelId)
        {
            try
            {
                // create a zip stream, leaving the target stream open after disposing
                using (var gzipStream = new GZipStream(trainedModelStream, CompressionMode.Decompress, leaveOpen: true))
                {
                    // deserialize the stream
                    var binaryFormatter = new BinaryFormatter();
                    object deserializedObject = binaryFormatter.Deserialize(gzipStream);
                    ITrainedModel trainedModel = deserializedObject as ITrainedModel;
                    if (trainedModel == null)
                    {
                        throw new Exception(
                            $"Unexpected object type found in stream. Found type {deserializedObject.GetType().Name}");
                    }

                    return trainedModel;
                }
            }
            catch (Exception ex)
            {
                var exception = new Exception($"Failed deserializing trained model {modelId} from stream", ex);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }
        
        /// <summary>
        /// Uploads a serializes trained model to blob storage
        /// </summary>
        private async Task UploadTrainedModelAsync(Stream modelStream, Guid modelId, CancellationToken cancellationToken)
        {
            try
            {
                // get the trained model blob name
                string modelBlobName = GetModelBlobName(modelId);

                Trace.TraceInformation($"Uploading the serialized trained model to blob '{modelBlobName}'");

                // upload the serialized model to blob
                await _modelsContainer.UploadBlobAsync(modelBlobName, modelStream, cancellationToken);
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Failed uploading trained model to blob storage. Model id: {modelId}",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Downloads a serialized trained model blob to a stream 
        /// </summary>
        private async Task DownloadTrainedModelAsync(Guid modelId, Stream target, CancellationToken cancellationToken)
        {
            // get the model blob name
            string modelBlobName = GetModelBlobName(modelId);

            try
            {
                // download the trained model to the target stream
                await _modelsContainer.DownloadBlobAsync(modelBlobName, target, cancellationToken);
            }
            catch (StorageException storageException)
            {
                if (storageException.RequestInformation?.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    var modelNotFoundException = new ModelNotFoundException(
                        $"Failed to find model blob '{modelBlobName}' in the model container", modelId,
                        storageException);
                    Trace.TraceWarning(modelNotFoundException.ToString());
                    throw modelNotFoundException;
                }

                var exception = new Exception($"Failed to download model '{modelId}' blob from the model container",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Gets or creates a <see cref="Recommender"/> for the input model
        /// </summary>
        private async Task<Recommender> GetOrCreateRecommenderAsync(Guid modelId, CancellationToken cancellationToken)
        {
            var key = modelId.ToString();
            var recommender = _recommendersCache.Get(key) as Recommender;
            if (recommender == null)
            {
                using (var stream = new MemoryStream())
                {
                    Trace.TraceInformation($"Downloading the serialized trained model '{modelId}' from blob storage");
                    await DownloadTrainedModelAsync(modelId, stream, cancellationToken);

                    // rewind the stream
                    stream.Seek(0, SeekOrigin.Begin);

                    Trace.TraceInformation($"Deserializing the trained model '{modelId}'");
                    ITrainedModel trainedModel = DeserializeTrainedModel(stream, modelId);

                    // get the model's user history store
                    IDocumentStore modelDocumentStore = _documentStoreProvider.GetDocumentStore(modelId);

                    // create a recommender from the trained model
                    recommender = new Recommender(trainedModel, modelDocumentStore, new Tracer(nameof(Recommender)));
                }
                
                bool result = _recommendersCache.Add(key, recommender,
                    new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1) });
                Trace.TraceVerbose($"Addition of model {modelId} recommender to the cache resulted with '{result}'");
            }

            return recommender;
        }
        
        /// <summary>
        /// Gets the name of the model's trained blob name  
        /// </summary>
        /// <param name="modelId">The model ID</param>
        /// <returns>The relative path to the model blob</returns>
        public static string GetModelBlobName(Guid modelId)
        {
            return Path.Combine(modelId.ToString(), "model.zip");
        }
        
        private class TrainingLocalFilePaths
        {
            /// <summary>
            /// Gets or sets the local catalog file path
            /// </summary>
            public string CatalogFilePath { get; set; }

            /// <summary>
            /// Gets or sets the local usage events folder path
            /// </summary>
            public string UsageFolderPath { get; set; }

            /// <summary>
            /// Gets or sets the local evaluation usage events folder path
            /// </summary>
            public string EvaluationUsageFolderPath { get; set; }
        }
        
        #endregion

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        /// <param name="disposing">Whether this method is called through Dispose()</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _recommendersCache.Dispose();
            }

            _disposed = true;
        }

        private readonly IBlobContainer _modelsContainer;
        private readonly IBlobContainerProvider _blobContainerProvider;
        private readonly IDocumentStoreProvider _documentStoreProvider;
        private readonly string _trainedModelsLocalRootPath;
        private readonly MemoryCache _recommendersCache = new MemoryCache(nameof(_recommendersCache));

        private const string UsageDirectoryName = "usage";
        private const string EvaluationUsageLocalDirectoryName = "evaluationUsage";
        internal const string ModelsBlobContainerName = "models";

        private static readonly ITracer Trace = new Tracer(nameof(ModelsProvider));
        private bool _disposed;
    }
}
