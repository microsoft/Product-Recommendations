using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Recommendations.Common;
using Recommendations.Common.Api;
using Recommendations.Common.Cloud;
using Recommendations.Core;
using Recommendations.WebApp.Models;

namespace Recommendations.UnitTest.Common
{
    [TestClass]
    [DeploymentItem("zlib.dll")]
    [DeploymentItem("CpuMathNative.dll")]
    public class ModelsProviderTest
    {
        [TestMethod]
        [DeploymentItem("TrainingFiles/small", nameof(TrainSmallUsageAsyncTest))]
        public async Task TrainSmallUsageAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var usageSource = Path.Combine(nameof(TrainSmallUsageAsyncTest), "usage");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();

            ProvideTrainBlobContainer(trainContainer, usageSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
            containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // train model
            var trainingParameters = CreateModelTrainingParameters(trainContainerName);
            var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
            Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");

            // expect the train blobs container to be listed
            await trainContainer.Received(1).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());

            // expect every usage blob to be downloaded
            await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);

            // expect a trained model to be uploaded to models container
            await ExpectUploadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/small", nameof(TrainSmallCatalogAndUsageWithEvaluationAsyncTest))]
        public async Task TrainSmallCatalogAndUsageWithEvaluationAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var catalogSource = Path.Combine(nameof(TrainSmallCatalogAndUsageWithEvaluationAsyncTest), "catalog.csv");
            var usageSource = Path.Combine(nameof(TrainSmallCatalogAndUsageWithEvaluationAsyncTest), "usage");
            var evaluationSource = Path.Combine(nameof(TrainSmallCatalogAndUsageWithEvaluationAsyncTest), "evaluation");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();

            ProvideTrainBlobContainer(trainContainer, usageSource, catalogSource, evaluationSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
            containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // train model
            var trainingParameters = CreateModelTrainingParameters(trainContainerName, true, true);
            var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
            Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");

            // expect the train blobs container to be listed
            await trainContainer.Received(1).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());
            await trainContainer.Received(1).ListBlobsAsync(EvaluationFolderRelativeLocation, Arg.Any<CancellationToken>());

            // expect every usage blob to be downloaded
            await ExpectDownloadBlobAsync(trainContainer, CatalogFileRelativeLocation);
            await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);
            await ExpectDownloadAllBlobsAsync(trainContainer, EvaluationFolderRelativeLocation, evaluationSource);

            // expect a trained model to be uploaded to models container
            await ExpectUploadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/small", nameof(TrainSmallCatalogAndSingleUsageFileWithSingleEvaluationFileAsyncTest))]
        public async Task TrainSmallCatalogAndSingleUsageFileWithSingleEvaluationFileAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var catalogSource = Path.Combine(nameof(TrainSmallCatalogAndSingleUsageFileWithSingleEvaluationFileAsyncTest), "catalog.csv");
            var usageSource = Path.Combine(nameof(TrainSmallCatalogAndSingleUsageFileWithSingleEvaluationFileAsyncTest), "usage");
            var evaluationSource = Path.Combine(nameof(TrainSmallCatalogAndSingleUsageFileWithSingleEvaluationFileAsyncTest), "evaluation");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();

            ProvideTrainBlobContainer(trainContainer, usageSource, catalogSource, evaluationSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
            containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // train model
            var trainingParameters = CreateModelTrainingParameters(trainContainerName, true, true);
            trainingParameters.UsageRelativePath = Path.Combine(trainingParameters.UsageRelativePath, "usage.csv");
            trainingParameters.EvaluationUsageRelativePath = Path.Combine(trainingParameters.EvaluationUsageRelativePath, "eval.csv");
            var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
            Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");

            // expect the train blobs to be check for existence
            await trainContainer.Received(1).ExistsAsync(trainingParameters.UsageRelativePath, Arg.Any<CancellationToken>());
            await trainContainer.Received(1).ExistsAsync(trainingParameters.EvaluationUsageRelativePath, Arg.Any<CancellationToken>());

            // expect the train blobs container to not be listed
            await trainContainer.Received(0).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());
            await trainContainer.Received(0).ListBlobsAsync(EvaluationFolderRelativeLocation, Arg.Any<CancellationToken>());

            // expect every usage blob to be downloaded
            await ExpectDownloadBlobAsync(trainContainer, CatalogFileRelativeLocation);
            await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);
            await ExpectDownloadAllBlobsAsync(trainContainer, EvaluationFolderRelativeLocation, evaluationSource);

            // expect a trained model to be uploaded to models container
            await ExpectUploadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/bad", nameof(TrainBadUsageAsyncTest))]
        public async Task TrainBadUsageAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var usageSource = Path.Combine(nameof(TrainBadUsageAsyncTest), "bad-usage");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();

            ProvideTrainBlobContainer(trainContainer, usageSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
            containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // train model
            var trainingParameters = CreateModelTrainingParameters(trainContainerName);
            var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
            Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");
            Assert.IsTrue(
                trainResult.UsageFilesParsingReport.Errors.Select(error => error.LineNumber)
                    .SequenceEqual(new long[] { 3, 19, 27, 37, 39, 40, 41, 42, 43, 44, 49, 56, 57, 98, 102 }),
                "UsageFilesParsingReport error lines are not as expected");

            // expect the train blobs container to be listed
            await trainContainer.Received(1).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());

            // expect every usage blob to be downloaded
            await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);

            // expect a trained model to be uploaded to models container
            await ExpectUploadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/bad", nameof(TrainBadUsageWithCatalogAndEvaluationAsyncTest))]
        public async Task TrainBadUsageWithCatalogAndEvaluationAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var catalogSource = Path.Combine(nameof(TrainBadUsageWithCatalogAndEvaluationAsyncTest), "good-catalog.csv");
            var usageSource = Path.Combine(nameof(TrainBadUsageWithCatalogAndEvaluationAsyncTest), "bad-usage");
            var evaluationSource = Path.Combine(nameof(TrainBadUsageWithCatalogAndEvaluationAsyncTest), "good-evaluation");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();

            ProvideTrainBlobContainer(trainContainer, usageSource, catalogSource, evaluationSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
            containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // train model
            var trainingParameters = CreateModelTrainingParameters(trainContainerName, true, true);
            var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
            Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");
            Assert.IsFalse(trainResult.CatalogFilesParsingReport.HasErrors,
                $"{nameof(trainResult.CatalogFilesParsingReport)} contains unexpected errors");
            Assert.IsFalse(trainResult.EvaluationFilesParsingReport.HasErrors,
                $"{nameof(trainResult.EvaluationFilesParsingReport)} contains unexpected errors");
            Assert.IsTrue(
                trainResult.UsageFilesParsingReport.Errors.Select(error => error.LineNumber)
                    .SequenceEqual(new long[] {3, 19, 27, 37, 39, 40, 41, 42, 43, 44, 49, 56, 57, 98, 102}),
                $"{nameof(trainResult.UsageFilesParsingReport)} error lines are not as expected");

            // expect the train blobs container to be listed
            await trainContainer.Received(1).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());
            await trainContainer.Received(1).ListBlobsAsync(EvaluationFolderRelativeLocation, Arg.Any<CancellationToken>());

            // expect every usage blob to be downloaded
            await ExpectDownloadBlobAsync(trainContainer, CatalogFileRelativeLocation);
            await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);
            await ExpectDownloadAllBlobsAsync(trainContainer, EvaluationFolderRelativeLocation, evaluationSource);

            // expect a trained model to be uploaded to models container
            await ExpectUploadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/bad", nameof(TrainBadCatalogAsyncTest))]
        public async Task TrainBadCatalogAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var catalogSource = Path.Combine(nameof(TrainBadCatalogAsyncTest), "bad-catalog.csv");
            var usageSource = Path.Combine(nameof(TrainBadCatalogAsyncTest), "good-usage");
            var evaluationSource = Path.Combine(nameof(TrainBadCatalogAsyncTest), "good-evaluation");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();

            ProvideTrainBlobContainer(trainContainer, usageSource, catalogSource, evaluationSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
            containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // train model
            var trainingParameters = CreateModelTrainingParameters(trainContainerName, true, true);
            var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
            Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");
            Assert.IsTrue(trainResult.CatalogFilesParsingReport.Errors.Select(error => error.LineNumber)
                .SequenceEqual(new long[] {3, 4, 5, 6, 7, 8, 10, 17}),
                "CatalogFilesParsingReport error lines are not as expected");

            // expect the train blobs container to be listed
            await trainContainer.Received(1).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());
            await trainContainer.Received(1).ListBlobsAsync(EvaluationFolderRelativeLocation, Arg.Any<CancellationToken>());

            // expect every usage blob to be downloaded
            await ExpectDownloadBlobAsync(trainContainer, CatalogFileRelativeLocation);
            await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);
            await ExpectDownloadAllBlobsAsync(trainContainer, EvaluationFolderRelativeLocation, evaluationSource);

            // expect a trained model to be uploaded to models container
            await ExpectUploadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/bad", nameof(TrainBadEvaluationAsyncTest))]
        public async Task TrainBadEvaluationAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var catalogSource = Path.Combine(nameof(TrainBadEvaluationAsyncTest), "good-catalog.csv");
            var usageSource = Path.Combine(nameof(TrainBadEvaluationAsyncTest), "good-usage");
            var evaluationSource = Path.Combine(nameof(TrainBadEvaluationAsyncTest), "bad-evaluation");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();

            ProvideTrainBlobContainer(trainContainer, usageSource, catalogSource, evaluationSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
            containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // train model
            var trainingParameters = CreateModelTrainingParameters(trainContainerName, true, true);
            var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
            Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");
            Assert.IsFalse(trainResult.CatalogFilesParsingReport.HasErrors,
                $"{nameof(trainResult.CatalogFilesParsingReport)} contains unexpected errors");
            Assert.IsFalse(trainResult.UsageFilesParsingReport.HasErrors,
                $"{nameof(trainResult.UsageFilesParsingReport)} contains unexpected errors");
            Assert.IsTrue(trainResult.EvaluationFilesParsingReport.Errors.Select(error => error.LineNumber)
                .SequenceEqual(new long[] {3, 19, 27, 37, 39, 40, 41, 42, 43, 44, 49, 56, 57, 98, 102}),
                $"{nameof(trainResult.EvaluationFilesParsingReport)} error lines are not as expected");

            // expect the train blobs container to be listed
            await trainContainer.Received(1).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());
            await trainContainer.Received(1).ListBlobsAsync(EvaluationFolderRelativeLocation, Arg.Any<CancellationToken>());

            // expect every usage blob to be downloaded
            await ExpectDownloadBlobAsync(trainContainer, CatalogFileRelativeLocation);
            await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);
            await ExpectDownloadAllBlobsAsync(trainContainer, EvaluationFolderRelativeLocation, evaluationSource);

            // expect a trained model to be uploaded to models container
            await ExpectUploadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/small", nameof(TrainAndScoreAsyncTest))]
        public async Task TrainAndScoreAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var trainContainer = Substitute.For<IBlobContainer>();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var catalogSource = Path.Combine(nameof(TrainAndScoreAsyncTest), "catalog.csv");
            var usageSource = Path.Combine(nameof(TrainAndScoreAsyncTest), "usage");
            var evaluationSource = Path.Combine(nameof(TrainAndScoreAsyncTest), "evaluation");
            var trainContainerName = "trainContainer";
            var modelId = Guid.NewGuid();
            var trainedModelBlobName = ModelsProvider.GetModelBlobName(modelId);

            using (var trainedModelStream = new MemoryStream())
            {
                AcceptBlob(modelsContainer, trainedModelBlobName, trainedModelStream);
                ProvideTrainBlobContainer(trainContainer, usageSource, catalogSource, evaluationSource);
                containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);
                containerProvider.GetBlobContainer(trainContainerName, Arg.Any<bool>()).Returns(trainContainer);

                // create model provider with substitutes
                var modelProvider = CreateModelProvider(containerProvider);

                // train model
                var trainingParameters = CreateModelTrainingParameters(trainContainerName, true, true);
                var trainResult = await modelProvider.TrainAsync(modelId, trainingParameters, null, CancellationToken.None);
                Assert.IsTrue(trainResult.IsCompletedSuccessfuly, "training failed did not complete successfully");

                // expect the train blobs container to be listed
                await trainContainer.Received(1).ListBlobsAsync(UsageFolderRelativeLocation, Arg.Any<CancellationToken>());
                await trainContainer.Received(1).ListBlobsAsync(EvaluationFolderRelativeLocation, Arg.Any<CancellationToken>());

                // expect every usage blob to be downloaded
                await ExpectDownloadBlobAsync(trainContainer, CatalogFileRelativeLocation);
                await ExpectDownloadAllBlobsAsync(trainContainer, UsageFolderRelativeLocation, usageSource);
                await ExpectDownloadAllBlobsAsync(trainContainer, EvaluationFolderRelativeLocation, evaluationSource);

                // expect a trained model to be uploaded to models container
                await ExpectUploadModelAsync(modelsContainer, modelId);

                // score with the trained model
                // provide uploaded stream
                trainedModelStream.Seek(0, SeekOrigin.Begin);
                modelsContainer.DownloadBlobAsync(trainedModelBlobName, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                    .Returns(args => trainedModelStream.CopyToAsync(args[1] as Stream));

                // get recommendations
                var recommendationsCount = 5;
                var scoreResult = await modelProvider.ScoreAsync(
                    modelId,
                    new[] { new UsageEvent { ItemId = "item.0" } },
                    null,
                    recommendationsCount, CancellationToken.None);

                // expect the requested number of recommendations
                Assert.AreEqual(recommendationsCount, scoreResult.Count, "did not receive the expected number of scoring results");

                // expect call to provider substitute
                await ExpectDownloadModelAsync(modelsContainer, modelId);
            }
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/small/model.zip", nameof(ScoreMultipleModelsAsyncTest))]
        public async Task ScoreMultipleModelsAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var modelId1 = Guid.NewGuid();
            var modelId2 = Guid.NewGuid();
            var modelId3 = Guid.NewGuid();
            var usageEvents = new[] {new UsageEvent {ItemId = "item.0"}};

            var trainedModelSource = Path.Combine(nameof(ScoreMultipleModelsAsyncTest), "model.zip");
            ProvideModel(modelsContainer, modelId1, trainedModelSource);
            ProvideModel(modelsContainer, modelId2, trainedModelSource);
            ProvideModel(modelsContainer, modelId3, trainedModelSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // get recommendations
            var scoreResult = await modelProvider.ScoreAsync(modelId1, usageEvents, null, 5, CancellationToken.None);
            Assert.AreEqual(5, scoreResult.Count, "did not receive the expected number of scoring results");

            scoreResult = await modelProvider.ScoreAsync(modelId2, usageEvents, null, 1, CancellationToken.None);
            Assert.AreEqual(1, scoreResult.Count, "did not receive the expected number of scoring results");

            scoreResult = await modelProvider.ScoreAsync(modelId1, usageEvents, null, 5, CancellationToken.None);
            Assert.AreEqual(5, scoreResult.Count, "did not receive the expected number of scoring results");

            scoreResult = await modelProvider.ScoreAsync(modelId2, usageEvents, null, 10, CancellationToken.None);
            Assert.AreEqual(10, scoreResult.Count, "did not receive the expected number of scoring results");

            // expect call to provider substitute
            await ExpectDownloadModelAsync(modelsContainer, modelId1);
            await ExpectDownloadModelAsync(modelsContainer, modelId2);
            await
                modelsContainer.DidNotReceive()
                    .DownloadBlobAsync(ModelsProvider.GetModelBlobName(modelId3), Arg.Any<Stream>(),
                        Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [DeploymentItem("TrainingFiles/small/model.zip", nameof(ScoreSmallModelAsyncTest))]
        public async Task ScoreSmallModelAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var modelId = Guid.NewGuid();

            var trainedModelSource = Path.Combine(nameof(ScoreSmallModelAsyncTest), "model.zip");
            ProvideModel(modelsContainer, modelId, trainedModelSource);
            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);

            // create model provider with substitutes
            var modelProvider = CreateModelProvider(containerProvider);

            // get recommendations
            int recommendationsCount = 5;
            var scoreResult = await modelProvider.ScoreAsync(
                modelId,
                new[] {new UsageEvent {ItemId = "item.0"}},
                null,
                recommendationsCount, CancellationToken.None);

            // expect the requested number of recommendations
            Assert.AreEqual(recommendationsCount, scoreResult.Count,
                "did not receive the expected number of scoring results");

            // expect call to provider substitute
            await ExpectDownloadModelAsync(modelsContainer, modelId);
        }

        [TestMethod]
        public async Task DeleteModelAsyncTest()
        {
            var modelsContainer = CreateModelContainer();
            var containerProvider = Substitute.For<IBlobContainerProvider>();
            var modelId = Guid.NewGuid();

            containerProvider.GetBlobContainer(ModelsProvider.ModelsBlobContainerName, Arg.Any<bool>()).Returns(modelsContainer);

            // create model provider with substitutes
            var modelsProvider = CreateModelProvider(containerProvider);

            // delete model
            await modelsProvider.DeleteModelAsync(modelId, CancellationToken.None);

            // expect call to provider substitute
            await ExpectDeleteModelAsync(modelsContainer, modelId);
        }

        #region Helpers

        private static IBlobContainer CreateModelContainer()
        {
            var modelBlobsContainer = Substitute.For<IBlobContainer>();

            // setup the models container to accept uploads
            modelBlobsContainer.UploadBlobAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0));

            // setup the models container to accept blob deletes
            modelBlobsContainer.DeleteBlobIfExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            return modelBlobsContainer;
        }

        private static ModelsProvider CreateModelProvider(IBlobContainerProvider blobContainerProvider, IDocumentStoreProvider documentStoreProvider = null)
        {
            var tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                Path.GetFileNameWithoutExtension(Path.GetTempFileName()));

            if (documentStoreProvider == null)
            {
                var emptyUserHistoryStore = Substitute.For<IDocumentStore>();
                emptyUserHistoryStore.GetDocument(Arg.Any<string>(), Arg.Any<string>()).Returns((Document)null);
                documentStoreProvider = Substitute.For<IDocumentStoreProvider>();
                documentStoreProvider.GetDocumentStore(Arg.Any<Guid>()).Returns(emptyUserHistoryStore);
            }

            return new ModelsProvider(blobContainerProvider, documentStoreProvider, tempFolder);
        }

        private static ModelTrainingParameters CreateModelTrainingParameters(string containerName,
            bool hasCatalog = false, bool hasEvaluationFiles = false)
        {
            var trainingParameters = ModelTrainingParameters.Default;
            trainingParameters.BlobContainerName = containerName;
            trainingParameters.UsageRelativePath = UsageFolderRelativeLocation;
            trainingParameters.CatalogFileRelativePath = hasCatalog ? CatalogFileRelativeLocation : null;
            trainingParameters.EvaluationUsageRelativePath = hasEvaluationFiles
                ? EvaluationFolderRelativeLocation
                : null;
            trainingParameters.EnableColdItemPlacement = hasCatalog;
            return trainingParameters;
        }

        private static void ProvideTrainBlobContainer(IBlobContainer blobContainer, string usageSourceDirectoryPath,
            string catalogSourceFilePath = null, string evaluationSourceDirectoryPath = null)
        {
            ProvideDirectory(blobContainer, UsageFolderRelativeLocation, usageSourceDirectoryPath);
            if (!string.IsNullOrWhiteSpace(catalogSourceFilePath))
            {
                ProvideBlob(blobContainer, CatalogFileRelativeLocation, catalogSourceFilePath);
            }
            if (!string.IsNullOrWhiteSpace(evaluationSourceDirectoryPath))
            {
                ProvideDirectory(blobContainer, EvaluationFolderRelativeLocation, evaluationSourceDirectoryPath);
            }
        }

        private static void ProvideModel(IBlobContainer blobContainer, Guid modelId, string modelSourceFilePath)
        {
            ProvideStream(blobContainer, ModelsProvider.GetModelBlobName(modelId), modelSourceFilePath);
        }

        private static void ProvideBlob(IBlobContainer blobContainer, string name, string sourceFilePath)
        {
            blobContainer.ExistsAsync(name, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
            blobContainer.DownloadBlobAsync(name, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(args =>
                {
                    File.Copy(sourceFilePath, args[1] as string);
                    return Task.FromResult(0);
                });
        }

        private static void ProvideStream(IBlobContainer blobContainer, string name, string sourceFilePath)
        {
            blobContainer.DownloadBlobAsync(name, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(args =>
                {
                    using (var reader = new FileStream(sourceFilePath, FileMode.Open))
                    {
                        reader.CopyTo((Stream)args[1]);
                    }

                    return Task.FromResult(0);
                });
        }

        private static void ProvideDirectory(IBlobContainer blobContainer, string name, string sourceDirectoryPath)
        {
            var localFiles = Directory.EnumerateFiles(sourceDirectoryPath).ToArray();
            var blobs = localFiles.Select(file => Path.Combine(name, Path.GetFileName(file))).ToArray();
            blobContainer.ListBlobsAsync(name, Arg.Any<CancellationToken>()).Returns(blobs);
            for (var i = 0; i < blobs.Length; i++)
            {
                ProvideBlob(blobContainer, blobs[i], localFiles[i]);
            }
        }

        private static void AcceptBlob(IBlobContainer modelContainer, string name, Stream destinationStream)
        {
            modelContainer.UploadBlobAsync(name, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(args => (args[1] as Stream).CopyToAsync(destinationStream));
        }

        private static async Task ExpectUploadModelAsync(IBlobContainer modelContainer, Guid modelId)
        {
            await
                modelContainer.Received(1)
                    .UploadBlobAsync(ModelsProvider.GetModelBlobName(modelId), Arg.Any<Stream>(),
                        Arg.Any<CancellationToken>());
        }

        private static async Task ExpectDownloadModelAsync(IBlobContainer modelContainer, Guid modelId)
        {
            await
                modelContainer.Received(1)
                    .DownloadBlobAsync(ModelsProvider.GetModelBlobName(modelId), Arg.Any<Stream>(),
                        Arg.Any<CancellationToken>());
        }

        private static async Task ExpectDeleteModelAsync(IBlobContainer modelContainer, Guid modelId)
        {
            await
                modelContainer.Received(1)
                    .DeleteBlobIfExistsAsync(ModelsProvider.GetModelBlobName(modelId), Arg.Any<CancellationToken>());
        }

        private static async Task ExpectDownloadBlobAsync(IBlobContainer blobContainer, string name)
        {
            await
                blobContainer.Received(1)
                    .DownloadBlobAsync(name, Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        private static async Task ExpectDownloadAllBlobsAsync(IBlobContainer blobContainer, string name, string sourceDirectoryPath)
        {
            var files = Directory.EnumerateFiles(sourceDirectoryPath).ToList();
            foreach (var file in files)
            {
                await
                    blobContainer.Received(1)
                        .DownloadBlobAsync(Path.Combine(name, Path.GetFileName(file)), Arg.Any<string>(),
                            Arg.Any<CancellationToken>());
            }
        }

        private const string UsageFolderRelativeLocation = "usage";
        private const string CatalogFileRelativeLocation = "catalog.csv";
        private const string EvaluationFolderRelativeLocation = "evaluation";

        #endregion
    }
}
