// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Recommendations.Common;
using Recommendations.Common.Api;
using Recommendations.Common.Cloud;

namespace Recommendations.UnitTest.Common
{
    [TestClass]
    public class ModelRegistryTest
    {
        [TestMethod]
        public async Task ListModelsAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            table.ListEntitiesAsync<ModelTableEntity>(CancellationToken.None, Arg.Any<string[]>())
                .Returns(Task.FromResult<IList<ModelTableEntity>>(new List<ModelTableEntity>()));
            var models = await modelsRegistry.ListModelsAsync(CancellationToken.None);
            Assert.IsNotNull(models);
            Assert.AreEqual(0, models.Count);

            table.ListEntitiesAsync<ModelTableEntity>(CancellationToken.None, Arg.Any<string[]>())
                .Returns(Task.FromResult<IList<ModelTableEntity>>(new[]
                {
                    new ModelTableEntity(Guid.NewGuid()),
                    new ModelTableEntity(Guid.NewGuid())
                }));
            models = await modelsRegistry.ListModelsAsync(CancellationToken.None);
            Assert.IsNotNull(models);
            Assert.AreEqual(2, models.Count);
        }

        [TestMethod]
        public async Task GetModelAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            var knownModelId = Guid.NewGuid();
            var unknownModelId = Guid.NewGuid();
            table.GetEntityAsync<ModelTableEntity>(knownModelId.ToString(), CancellationToken.None, Arg.Any<string[]>())
                .Returns(Task.FromResult(new ModelTableEntity(knownModelId)));

            // Valid Model Id
            var model = await modelsRegistry.GetModelAsync(knownModelId, CancellationToken.None);
            Assert.IsNotNull(model);
            Assert.AreEqual(knownModelId, model.Id);

            // Invalid Model Id
            model = await modelsRegistry.GetModelAsync(unknownModelId, CancellationToken.None);
            Assert.IsNull(model);
        }

        [TestMethod]
        public async Task GetDefaultModelIdAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            // Default model not present in Models
            var knownModelId = Guid.NewGuid();
            table.GetEntityAsync<ModelIdTableEntity>(ModelsRegistry.DefaultModelIdKeyName, CancellationToken.None,
                nameof(ModelIdTableEntity.ModelId))
                .Returns(Task.FromResult(new ModelIdTableEntity(ModelsRegistry.DefaultModelIdKeyName, knownModelId)));
            var model = await modelsRegistry.GetDefaultModelAsync(CancellationToken.None);
            Assert.IsNull(model);

            // Default model present in Models
            table.GetEntityAsync<ModelTableEntity>(knownModelId.ToString(), CancellationToken.None, Arg.Any<string[]>())
                .Returns(Task.FromResult(new ModelTableEntity(knownModelId)));
            model = await modelsRegistry.GetDefaultModelAsync(CancellationToken.None);
            Assert.IsNotNull(model);
            Assert.AreEqual(knownModelId, model.Id);
        }

        [TestMethod]
        public async Task SetDefaultModelIdAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            // Scenario 1: Invalid Model
            // Validate set api is not called
            table.GetEntityAsync<ModelTableEntity>(Arg.Any<string>(), CancellationToken.None, Arg.Any<string[]>())
                .Returns(Task.FromResult<ModelTableEntity>(null));
            table.InsertOrReplaceEntityAsync(Arg.Any<ModelTableEntity>(), CancellationToken.None)
                .Returns(Task.FromResult(true));
            var unknownId = Guid.NewGuid();
            bool result = await modelsRegistry.SetDefaultModelIdAsync(unknownId, CancellationToken.None);
            Assert.IsFalse(result);
            await table.DidNotReceive().InsertOrReplaceEntityAsync(Arg.Any<ModelIdTableEntity>(), Arg.Any<CancellationToken>());

            // Scenario 2: Valid Model but Status not Complete 
            // Validate set api is not called
            var knownId = Guid.NewGuid();
            table.GetEntityAsync<ModelTableEntity>(knownId.ToString(), CancellationToken.None, Arg.Any<string[]>())
                .Returns(
                    Task.FromResult(new ModelTableEntity(knownId) {ModelStatus = ModelStatus.InProgress.ToString()}));

            result = await modelsRegistry.SetDefaultModelIdAsync(knownId, CancellationToken.None);
            Assert.IsFalse(result);
            await table.DidNotReceive()
                .InsertOrReplaceEntityAsync(Arg.Any<ModelIdTableEntity>(), Arg.Any<CancellationToken>());

            // Scenario 3: Valid Model with Status as Complete
            // Validate set api is called
            table.GetEntityAsync<ModelTableEntity>(knownId.ToString(), CancellationToken.None, Arg.Any<string[]>())
                .Returns(
                    Task.FromResult(new ModelTableEntity(knownId) { ModelStatus = ModelStatus.Completed.ToString()}));
            table.InsertOrReplaceEntityAsync(Arg.Any<ModelIdTableEntity>(), CancellationToken.None).Returns(Task.FromResult(true));
            result = await modelsRegistry.SetDefaultModelIdAsync(knownId, CancellationToken.None);
            Assert.IsTrue(result);
            await table.Received(1).InsertOrReplaceEntityAsync(
                Arg.Is<ModelIdTableEntity>(
                    entity => entity.RowKey == ModelsRegistry.DefaultModelIdKeyName && entity.ModelId == knownId),
                CancellationToken.None);
        }

        [TestMethod]
        public async Task ClearDefaultModelIdAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            // Scenario 1: Model is set as default. Should return true after clearing.
            table.DeleteEntityAsync<ModelIdTableEntity>(ModelsRegistry.DefaultModelIdKeyName, CancellationToken.None)
                .Returns(Task.FromResult(true));
            Assert.IsTrue(await modelsRegistry.ClearDefaultModelIdAsync(CancellationToken.None));
            await table.Received(1).DeleteEntityAsync<ModelIdTableEntity>(ModelsRegistry.DefaultModelIdKeyName, CancellationToken.None);

            // Scenario 2: Table is unable to delete entity
            table.ClearReceivedCalls();
            table.DeleteEntityAsync<ModelIdTableEntity>(Arg.Any<string>(), CancellationToken.None)
                .Returns(Task.FromResult(false));
            Assert.IsFalse(await modelsRegistry.ClearDefaultModelIdAsync(CancellationToken.None));
            await table.Received(1).DeleteEntityAsync<ModelIdTableEntity>(ModelsRegistry.DefaultModelIdKeyName, CancellationToken.None);
        }

        [TestMethod]
        public async Task CreateModelAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            table.InsertOrReplaceEntityAsync(Arg.Any<ModelIdTableEntity>(), CancellationToken.None).Returns(Task.FromResult(true));
            table.InsertEntityAsync(Arg.Any<ModelTableEntity>(), CancellationToken.None).Returns(Task.FromResult(true));

            Model model = await modelsRegistry.CreateModelAsync(ModelTrainingParameters.Default, null, CancellationToken.None);
            await table.Received(1).InsertEntityAsync(
                Arg.Is<ModelTableEntity>(me => Guid.Parse(me.RowKey) == model.Id && me.ModelStatus == ModelStatus.Created.ToString()),
                CancellationToken.None);
        }

        [TestMethod]
        public async Task UpdateModelAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            table.MergeEntityAsync(Arg.Any<ModelTableEntity>(), CancellationToken.None).Returns(Task.FromResult(true));

            var modelId = Guid.NewGuid();

            // Scenario 1: All variables null. nothing should be overridden.
            await modelsRegistry.UpdateModelAsync(modelId, CancellationToken.None);
            await table.Received(1).MergeEntityAsync(
                Arg.Is<ModelTableEntity>(
                    me => Guid.Parse(me.RowKey) == modelId && me.ModelStatus == null && me.StatusMessage == null && me.ModelStatistics == null),
                CancellationToken.None);

            // Scenario 2: Only status needs to be updated
            table.ClearReceivedCalls();
            await modelsRegistry.UpdateModelAsync(modelId, CancellationToken.None, ModelStatus.InProgress);
            await table.Received(1).MergeEntityAsync(
                Arg.Is<ModelTableEntity>(
                    me =>
                        Guid.Parse(me.RowKey) == modelId && me.ModelStatus == ModelStatus.InProgress.ToString() &&
                        me.StatusMessage == null && me.ModelStatistics == null), CancellationToken.None);

            // Scenario 3: Only status message needs to be updated
            table.ClearReceivedCalls();
            await modelsRegistry.UpdateModelAsync(modelId, CancellationToken.None, null, "test");
            await table.Received(1).MergeEntityAsync(
                Arg.Is<ModelTableEntity>(
                    me => Guid.Parse(me.RowKey) == modelId && me.ModelStatus == null && me.StatusMessage == "test" && me.ModelStatistics == null),
                CancellationToken.None);

            // Scenario 4: Only statistics needs to be updated
            var modelStatistics = new ModelStatistics
            {
                NumberOfCatalogItems = 10
            };
            table.ClearReceivedCalls();
            await modelsRegistry.UpdateModelAsync(modelId, CancellationToken.None, null, null, modelStatistics);
            await table.Received(1).MergeEntityAsync(
                Arg.Is<ModelTableEntity>(
                    me =>
                        Guid.Parse(me.RowKey) == modelId && me.ModelStatus == null && me.StatusMessage == null &&
                        me.ModelStatistics == JsonConvert.SerializeObject(modelStatistics)),
                CancellationToken.None);

            // Scenario 5: everything needs to be updated
            table.ClearReceivedCalls();
            await modelsRegistry.UpdateModelAsync(modelId, CancellationToken.None, ModelStatus.Completed, "test", modelStatistics);
            await table.Received(1).MergeEntityAsync(
                Arg.Is<ModelTableEntity>(
                    me =>
                        Guid.Parse(me.RowKey) == modelId && me.ModelStatus == ModelStatus.Completed.ToString() &&
                        me.StatusMessage == "test" && me.ModelStatistics == JsonConvert.SerializeObject(modelStatistics)),
                CancellationToken.None);
        }

        [TestMethod]
        public async Task DeleteModelIfExistsAsyncTest()
        {
            ITable table = Substitute.For<ITable>();
            var modelsRegistry = new ModelsRegistry(table);

            var knownModelId = Guid.NewGuid();
            table.DeleteEntityAsync<ModelTableEntity>(knownModelId.ToString(), CancellationToken.None)
                .Returns(Task.FromResult(true));

            bool result = await modelsRegistry.DeleteModelIfExistsAsync(knownModelId, CancellationToken.None);
            Assert.IsTrue(result);
            await table.Received(1)
                .DeleteEntityAsync<ModelTableEntity>(Arg.Is<string>(id => Guid.Parse(id) == knownModelId), CancellationToken.None);

            var unknownModelId = Guid.NewGuid();
            result = await modelsRegistry.DeleteModelIfExistsAsync(unknownModelId, CancellationToken.None);
            Assert.IsFalse(result);
            await table.Received(1).DeleteEntityAsync<ModelTableEntity>(
                Arg.Is<string>(id => Guid.Parse(id) == unknownModelId), CancellationToken.None);
        }
    }
}
