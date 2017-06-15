// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Recommendations.Common;
using Recommendations.Common.Api;
using Recommendations.Common.Cloud;
using Recommendations.Core;
using Recommendations.WebApp.Models;
using Swashbuckle.Swagger.Annotations;

namespace Recommendations.WebApp.Controllers
{
    /// <summary>
    /// A controller for creating and managing recommendations models 
    /// </summary>
    [ApiKeyAuthorizationFilter(AuthorizationAppSettingsKeys.AdminPrimaryKey, AuthorizationAppSettingsKeys.AdminSecondaryKey)]
    public class ModelsController : ApiController
    {
        /// <summary>
        /// Lists all the models
        /// </summary>
        [Route("api/models", Name = nameof(GetModels))]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(IList<Model>)), SwaggerResponseRemoveDefaults]
        public async Task<IHttpActionResult> GetModels(CancellationToken cancellationToken)
        {
            Trace.TraceVerbose("Listing all models in the registry");
            ModelsRegistry modelsRegistry = WebAppContext.ModelsRegistry;
            IList<Model> models = await modelsRegistry.ListModelsAsync(cancellationToken);
            return Ok(models);
        }

        /// <summary>
        /// Gets a model by id
        /// </summary>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <param name="modelId">The model id to retrieve</param>
        [Route("api/models/{modelId}", Name = nameof(GetModel))]
        [HttpGet]
        [ResponseType(typeof (Model))]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof (Model)), SwaggerResponseRemoveDefaults]
        public async Task<IHttpActionResult> GetModel(CancellationToken cancellationToken, [FromUri] Guid? modelId)
        {
            if (!modelId.HasValue)
            {
                var message = $"{nameof(modelId)} is not valid";
                Trace.TraceVerbose(message);
                return BadRequest(message);
            }

            // set the model id to context
            ContextManager.ModelId = modelId;

            Trace.TraceVerbose($"Trying to read the model '{modelId}' from the registry");
            ModelsRegistry modelsRegistry = WebAppContext.ModelsRegistry;
            Model model = await modelsRegistry.GetModelAsync(modelId.Value, cancellationToken);
            if (model == null)
            {
                Trace.TraceInformation($"Model with id '{modelId}' does not exists.");
                return NotFound();
            }

            return Ok(model);
        }

        /// <summary>
        /// Retrieve the default model.
        /// </summary>
        [Route("api/models/default", Name = nameof(GetDefaultModel))]
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(Model)), SwaggerResponseRemoveDefaults]
        public async Task<IHttpActionResult> GetDefaultModel(CancellationToken cancellationToken)
        {
            // set the model id to context
            ContextManager.ModelId = "default";

            Trace.TraceVerbose("Trying to read the default model from the registry");
            ModelsRegistry modelsRegistry = WebAppContext.ModelsRegistry;
            Model defaultModel = await modelsRegistry.GetDefaultModelAsync(cancellationToken);
            if (defaultModel == null)
            {
                Trace.TraceInformation("A default model is not defined");
                return NotFound();
            }

            return Ok(defaultModel);
        }

        /// <summary>
        /// Sets a model as the default model 
        /// </summary>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <param name="modelId">The model id to set as default</param>
        [Route("api/models/default", Name = nameof(SetDefaultModel))]
        [HttpPut]
        [SwaggerResponse(HttpStatusCode.OK), SwaggerResponseRemoveDefaults]
        public async Task<IHttpActionResult> SetDefaultModel(CancellationToken cancellationToken, Guid? modelId)
        {
            if (!modelId.HasValue)
            {
                var message = $"{nameof(modelId)} is not valid";
                Trace.TraceVerbose(message);
                return BadRequest(message);
            }

            // set the model id to context
            ContextManager.ModelId = modelId;

            Trace.TraceVerbose($"Trying to set '{modelId}' as the default model in the registry");
            ModelsRegistry modelsRegistry = WebAppContext.ModelsRegistry;
            bool result = await modelsRegistry.SetDefaultModelIdAsync(modelId.Value, cancellationToken);
            if (!result)
            {
                Trace.TraceInformation($"Failed setting model with id '{modelId}' as the default model");
                return NotFound();
            }

            return Ok();
        }

        /// <summary>
        /// Trains a new model
        /// </summary>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <param name="modelParameters">The new model parameters</param>
        [Route("api/models", Name = nameof(TrainNewModel))]
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.Created, Type = typeof(Model)), SwaggerResponseRemoveDefaults]
        public async Task<IHttpActionResult> TrainNewModel(CancellationToken cancellationToken, [FromBody]ModelParameters modelParameters)
        {
            // validate input
            if (modelParameters == null)
            {
                var message = $"Invalid format. Expected a valid '{nameof(ModelParameters)}' JSON";
                Trace.TraceVerbose(message);
                return BadRequest(message);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ModelsRegistry modelsRegistry = WebAppContext.ModelsRegistry;

            Trace.TraceVerbose("Converting the model parameters to trainer settings, using default values where needed");
            var @default = ModelTrainingParameters.Default;
            var settings = new ModelTrainingParameters
            {
                BlobContainerName = modelParameters.BlobContainerName,
                CatalogFileRelativePath = modelParameters.CatalogFileRelativePath?.Replace('\\', '/').Trim('/'),
                UsageRelativePath = modelParameters.UsageRelativePath?.Replace('\\', '/').Trim('/'),
                EvaluationUsageRelativePath = modelParameters.EvaluationUsageRelativePath?.Replace('\\', '/').Trim('/'),
                SupportThreshold = modelParameters.SupportThreshold ?? @default.SupportThreshold,
                CooccurrenceUnit = modelParameters.CooccurrenceUnit ?? @default.CooccurrenceUnit,
                SimilarityFunction = modelParameters.SimilarityFunction ?? @default.SimilarityFunction,
                EnableColdItemPlacement = modelParameters.EnableColdItemPlacement ?? @default.EnableColdItemPlacement,
                EnableColdToColdRecommendations =
                    modelParameters.EnableColdToColdRecommendations ?? @default.EnableColdToColdRecommendations,
                EnableUserAffinity = modelParameters.EnableUserAffinity ?? @default.EnableUserAffinity,
                EnableUserToItemRecommendations =
                    modelParameters.EnableUserToItemRecommendations ?? @default.EnableUserToItemRecommendations,
                AllowSeedItemsInRecommendations =
                    modelParameters.AllowSeedItemsInRecommendations ?? @default.AllowSeedItemsInRecommendations,
                EnableBackfilling = modelParameters.EnableBackfilling ?? @default.EnableBackfilling,
                DecayPeriodInDays = modelParameters.DecayPeriodInDays ?? @default.DecayPeriodInDays
            };

            Trace.TraceInformation("Creating new model in registry");
            Model model = await modelsRegistry.CreateModelAsync(settings, modelParameters.Description, cancellationToken);

            Trace.TraceInformation($"Queueing a new train model message to the queue for model id {model.Id}");
            ModelQueueMessage modelQueueMessage = new ModelQueueMessage { ModelId = model.Id };
            await WebAppContext.TrainModelQueue.AddMessageAsync(modelQueueMessage, cancellationToken);

            // return the URL to the created model
            return CreatedAtRoute(nameof(GetModel), new {modelId = model.Id}, model);
        }

        /// <summary>
        /// Delete an existing model by id
        /// </summary>
        /// <param name="cancellationToken">The cancellation token assigned for the operation.</param>
        /// <param name="modelId">The model to delete</param>
        [Route("api/models/{modelId}", Name = nameof(DeleteModel))]
        [HttpDelete]
        [SwaggerResponse(HttpStatusCode.Accepted), SwaggerResponseRemoveDefaults]
        public async Task<IHttpActionResult> DeleteModel(CancellationToken cancellationToken, [FromUri] Guid? modelId)
        {
            if (!modelId.HasValue)
            {
                var message = $"{nameof(modelId)} is not valid";
                Trace.TraceVerbose(message);
                return BadRequest(message);
            }

            // set the model id to context
            ContextManager.ModelId = modelId;

            // clear default model table if this is default model
            var modelsRegistry = WebAppContext.ModelsRegistry;
            var defaultModelId = await modelsRegistry.GetDefaultModelIdAsync(cancellationToken);
            if (defaultModelId.Equals(modelId))
            {
                Trace.TraceInformation($"Unsetting model '{modelId}' from being the default model");
                await modelsRegistry.ClearDefaultModelIdAsync(cancellationToken);
            }

            Trace.TraceInformation($"Deleting model {modelId}");
            var result = await modelsRegistry.DeleteModelIfExistsAsync(modelId.Value, cancellationToken);
            if (!result)
            {
                Trace.TraceWarning($"'{nameof(DeleteModel)}' API - Model with id '{modelId}' was not found.");
                return NotFound();
            }

            Trace.TraceVerbose("Adding a delete model message to the queue");
            await WebAppContext.DeleteModelQueue.AddMessageAsync(
                new ModelQueueMessage {ModelId = modelId.Value}, cancellationToken);

            return StatusCode(HttpStatusCode.Accepted);
        }

        private static readonly ITracer Trace = new Tracer(nameof(ModelsController));
    }
}
