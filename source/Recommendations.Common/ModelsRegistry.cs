// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Recommendations.Common.Api;
using Recommendations.Common.Cloud;
using Recommendations.Core;

namespace Recommendations.Common
{
    /// <summary>
    /// A class for storing models in an entity table
    /// </summary>
    public class ModelsRegistry : IDisposable
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ModelsRegistry"/> class.
        /// </summary>
        /// <param name="modelsTable">The underlying models table</param>
        public ModelsRegistry(ITable modelsTable)
        {
            if (modelsTable == null)
            {
                throw new ArgumentNullException(nameof(modelsTable));
            }

            _modelsTable = modelsTable;
        }

        /// <summary>
        /// Lists all the registered models
        /// </summary>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        public async Task<IList<Model>> ListModelsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                Trace.TraceInformation("Listing all the model entities, getting only the model status, description and creation time");
                IList<ModelTableEntity> entities = await _modelsTable.ListEntitiesAsync<ModelTableEntity>(cancellationToken, 
                    nameof(ModelTableEntity.Description), nameof(ModelTableEntity.CreationTime), nameof(ModelTableEntity.ModelStatus));

                // extract and return the models
                return entities.Select(ConvertEntityToModel).ToList();
            }
            catch (StorageException storageException)
            {
                var exception = new Exception("Exception while trying to list models from table", storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }
        
        /// <summary>
        /// Retrieves a model by id
        /// </summary>
        /// <param name="modelId">The model id to retrieve</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <returns>The found model or <value>null</value> if not found</returns>
        public async Task<Model> GetModelAsync(Guid modelId, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                Trace.TraceInformation($"Getting model '{modelId}' entity from the table"); 
                ModelTableEntity modelEntity =
                    await _modelsTable.GetEntityAsync<ModelTableEntity>(
                        modelId.ToString(),
                        cancellationToken,
                        nameof(ModelTableEntity.Description),
                        nameof(ModelTableEntity.CreationTime),
                        nameof(ModelTableEntity.ModelStatus),
                        nameof(ModelTableEntity.StatusMessage),
                        nameof(ModelTableEntity.ModelParameters),
                        nameof(ModelTableEntity.ModelStatistics));
                
                // convert and return 
                return ConvertEntityToModel(modelEntity);
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Exception while trying to get model ({modelId}) entity from table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Retrieves a model by id synchronously.
        /// </summary>
        /// <param name="modelId">The model id to retrieve</param>
        /// <returns>The found model or <value>null</value> if not found</returns>
        public Model GetModel(Guid modelId)
        {
            ThrowIfDisposed();

            try
            {
                Trace.TraceVerbose($"Getting model '{modelId}' entity from the table");
                ModelTableEntity modelEntity =
                    _modelsTable.GetEntity<ModelTableEntity>(
                        modelId.ToString(),
                        nameof(ModelTableEntity.Description),
                        nameof(ModelTableEntity.CreationTime),
                        nameof(ModelTableEntity.ModelStatus),
                        nameof(ModelTableEntity.StatusMessage),
                        nameof(ModelTableEntity.ModelParameters),
                        nameof(ModelTableEntity.ModelStatistics));

                // convert and return 
                return ConvertEntityToModel(modelEntity);
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Exception while trying to get model ({modelId}) entity from table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Retrieves a model by id
        /// </summary>
        /// <param name="modelId">The model id to retrieve</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <returns>The found model or <value>null</value> if not found</returns>
        public async Task<ModelStatus?> GetModelStatusAsync(Guid modelId, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                Trace.TraceInformation($"Trying to get the model '{modelId}' status from the cache");
                ModelStatus? modelStatus = _cache.Get(modelId.ToString()) as ModelStatus?;
                if (modelStatus.HasValue)
                {
                    return modelStatus;
                }

                Trace.TraceInformation($"Getting model '{modelId}' status from the table");
                ModelTableEntity entity =
                    await _modelsTable.GetEntityAsync<ModelTableEntity>(
                        modelId.ToString(), cancellationToken, nameof(ModelTableEntity.ModelStatus));

                ModelStatus status;
                if (!string.IsNullOrWhiteSpace(entity?.ModelStatus) && Enum.TryParse(entity.ModelStatus, out status))
                {
                    modelStatus = status;

                    // only update the cache if the model state is 'stable'
                    if (modelStatus == ModelStatus.Completed || modelStatus == ModelStatus.Failed)
                    {
                        _cache.Set(modelId.ToString(), modelStatus,
                            new CacheItemPolicy {AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)});
                    }
                }

                Trace.TraceInformation($"Found model '{modelId}' status: {modelStatus}");
                return modelStatus;
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Exception while trying to get model ({modelId}) status from table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Gets the default model id.
        /// </summary>
        /// <returns>The default model id or <value>null</value> if a default model is not defined</returns>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        public async Task<Guid?> GetDefaultModelIdAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                Trace.TraceInformation("Getting the default model id from the table");
                ModelIdTableEntity entity = await _modelsTable.GetEntityAsync<ModelIdTableEntity>(
                    DefaultModelIdKeyName, cancellationToken, nameof(ModelIdTableEntity.ModelId));

                Guid? defaultModelId = entity?.ModelId;
                Trace.TraceInformation($"Found the default model id {defaultModelId}");
                return defaultModelId;
            }
            catch (StorageException storageException)
            {
                var exception = new Exception("Exception while trying to get default model id entity from table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Sets a model id as the default model
        /// </summary>
        /// <param name="modelId">The model id to be set as default</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <returns>
        /// <value>true</value> if the model was set as default or <value>false</value> if 
        /// the model was not found or its status is not <see cref="ModelStatus.Completed"/>
        /// </returns>
        public async Task<bool> SetDefaultModelIdAsync(Guid modelId, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            Model newDefaultModel = await GetModelAsync(modelId, cancellationToken);
            if (newDefaultModel?.Status != ModelStatus.Completed)
            {
                return false;
            }

            // create a default model entity using the model id
            var defaultModelEntity = new ModelIdTableEntity(DefaultModelIdKeyName, newDefaultModel.Id);

            try
            {
                Trace.TraceInformation($"Setting the default model id to '{modelId}'");
                bool result =  await _modelsTable.InsertOrReplaceEntityAsync(defaultModelEntity, cancellationToken);

                Trace.TraceInformation($"Default model id updated to '{modelId}' returned with '{result}'");
                return result;
            }
            catch (StorageException storageException)
            {
                var exception = new Exception(
                    $"Exception while trying to update default model id ({newDefaultModel.Id}) entity to the table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Unset the default model
        /// </summary>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <returns>
        /// <value>true</value> if the model was set as default or <value>false</value> if 
        /// the model was not found or its status is not <see cref="ModelStatus.Completed"/>
        /// </returns>
        public async Task<bool> ClearDefaultModelIdAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                Trace.TraceInformation("Unsetting the default mode id");
                return await _modelsTable.DeleteEntityAsync<ModelIdTableEntity>(DefaultModelIdKeyName, cancellationToken);
            }
            catch (StorageException storageException)
            {
                var exception = new Exception("Exception while trying to clear default model id in the table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Retrieve the default model.
        /// </summary>
        /// <returns>The default model or <value>null</value> if an default model was not found</returns>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        public async Task<Model> GetDefaultModelAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            // get the default model id
            Guid? defaultModelId = await GetDefaultModelIdAsync(cancellationToken);
            if (!defaultModelId.HasValue)
            {
                // no default model is set
                Trace.TraceInformation("Default model is not set");
                return null;
            }

            Trace.TraceInformation($"Get the default model ({defaultModelId.Value}) form the table");
            return await GetModelAsync(defaultModelId.Value, cancellationToken);
        }

        /// <summary>
        /// Creates a new model
        /// </summary>
        /// <param name="modelParameters">The new model parameters</param>
        /// <param name="description">The new model description</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <returns>The newly created <see cref="Model"/> or <value>null</value> if failed to create</returns>
        public async Task<Model> CreateModelAsync(ModelTrainingParameters modelParameters, string description, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (modelParameters == null)
            {
                throw new ArgumentNullException(nameof(modelParameters));
            }

            // allocate a new model id
            var modelId = Guid.NewGuid();

            // create a new model entity
            var newModelEntity = new ModelTableEntity(modelId, modelParameters)
            {
                Description = description,
                ModelStatus = ModelStatus.Created.ToString(),
                CreationTime = DateTime.UtcNow
            };
            
            try
            {
                Trace.TraceInformation($"Creating a new model ({modelId}) in the table");
                if (!await _modelsTable.InsertEntityAsync(newModelEntity, cancellationToken))
                {
                    Trace.TraceError($"Failed to create table entry for model {modelId}");
                    return null;
                }
            }
            catch (StorageException storageException)
            {
                var exception = new Exception(
                    $"Exception while trying to create a new model entity ({modelId}) in the table", storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }

            // convert the entity to model
            Model newModel = ConvertEntityToModel(newModelEntity);
            return newModel;
        }

        /// <summary>
        /// Updates a model by overriding the provided non-null properties.
        /// </summary>
        /// <param name="modelId">The id of the model to update</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <param name="status">A new status to update</param>
        /// <param name="statusMessage">A status message</param>
        /// <param name="statistics">Optional new model statistics to update</param>
        public async Task UpdateModelAsync(Guid modelId, CancellationToken cancellationToken,
            ModelStatus? status = null, string statusMessage = null, ModelStatistics statistics = null)
        {
            ThrowIfDisposed();

            try
            {
                // create a model entity
                var modelEntity = new ModelTableEntity(modelId, null, statistics) {ETag = "*"};
                    
                // set the model status (if provided)
                if (status.HasValue)
                {
                    modelEntity.ModelStatus = status.Value.ToString();
                }

                // set the model status message (if provided)
                if (statusMessage != null)
                {
                    if (statusMessage.Length > MaxAllowedStatusMessageLength)
                    {
                        statusMessage = statusMessage.Substring(0, MaxAllowedStatusMessageLength) + "... [trimmed]";
                    }

                    modelEntity.StatusMessage = statusMessage;
                }

                Trace.TraceInformation($"Updating model '{modelId}' properties in the table");

                // override only the non-null properties of the update model entity to the table
                await _modelsTable.MergeEntityAsync(modelEntity, cancellationToken);
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Exception while trying to update model entity ({modelId}) to the table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Deletes a model by id if it still exists
        /// </summary>
        /// <param name="modelId">the model id to delete</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <returns><value>true</value> if the model existed and was deleted, <value>false</value> otherwise</returns>
        public async Task<bool> DeleteModelIfExistsAsync(Guid modelId, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            try
            {
                Trace.TraceInformation($"Deleting model '{modelId}' from the table");
                bool result = await _modelsTable.DeleteEntityAsync<ModelTableEntity>(modelId.ToString(), cancellationToken);

                // remove the model from the cache (if exists)
                _cache.Remove(modelId.ToString());
                
                return result;
            }
            catch (StorageException storageException)
            {
                var exception = new Exception($"Exception while trying to delete model {modelId} from the table",
                    storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Disposes the model registry and it's cache
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the model registry and it's cache
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _cache.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ModelsRegistry));
            }
        }

        /// <summary>
        /// Converts the entity to a <see cref="Model"/> instance.
        /// </summary>
        private static Model ConvertEntityToModel(ModelTableEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            // create a model from the entity properties
            var model = new Model
            {
                Id = Guid.Parse(entity.RowKey),
                Description = entity.Description,
                CreationTime = entity.CreationTime,
                Status = default(ModelStatus),
                StatusMessage = entity.StatusMessage,
            };

            // try to parse the model status
            ModelStatus status;
            if (!string.IsNullOrWhiteSpace(entity.ModelStatus) && Enum.TryParse(entity.ModelStatus, out status))
            {
                model.Status = status;
            }

            try
            {
                // deserialize the model parameters
                if (!string.IsNullOrWhiteSpace(entity.ModelParameters))
                {
                    model.Parameters = JsonConvert.DeserializeObject<ModelTrainingParameters>(entity.ModelParameters);
                }
            }
            catch (JsonSerializationException ex)
            {
                Trace.TraceError($"Failed deserializing {nameof(ModelTrainingParameters)} for model {entity.RowKey}. Exception: {ex}");
            }

            try
            {
                // deserialize the model statistics
                if (!string.IsNullOrWhiteSpace(entity.ModelStatistics))
                {
                    model.Statistics = JsonConvert.DeserializeObject<ModelStatistics>(entity.ModelStatistics);
                }
            }
            catch (JsonSerializationException ex)
            {
                Trace.TraceError($"Failed deserializing {nameof(ModelStatistics)} for model {entity.RowKey}. Exception: {ex}");
            }

            // return the model
            return model;
        }

        private bool _disposed;
        private readonly ITable _modelsTable;
        private readonly MemoryCache _cache = new MemoryCache(nameof(ModelsRegistry));
        private static readonly ITracer Trace = new Tracer(nameof(ModelsRegistry));

        internal const string DefaultModelIdKeyName = "DefaultModelId";
        internal const int MaxAllowedStatusMessageLength = 10000;
    }
}
