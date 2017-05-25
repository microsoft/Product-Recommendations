// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Recommendations.Common.Api;
using Recommendations.Core;
using Recommendations.Core.Train;

namespace Recommendations.UnitTest.Core
{
    [TestClass]
    [DeploymentItem("zlib.dll")]
    [DeploymentItem("CpuMathNative.dll")]
    public class ModelTrainerTests
    {
        [TestMethod]
        public void TrainSmallModelUsingDefaultParametersTest()
        {
            const string baseFolder = nameof(TrainSmallModelUsingDefaultParametersTest);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator();
            string usageFileFolderPath = Path.Combine(baseFolder, "usage");
            Directory.CreateDirectory(usageFileFolderPath);
            generator.CreateUsageFile(Path.Combine(usageFileFolderPath, "usage.csv"), 100);

            var trainer = new ModelTrainer();
            ModelTrainResult result = trainer.TrainModel(ModelTrainingParameters.Default, usageFileFolderPath, null, null, CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompletedSuccessfuly);
            Assert.IsNull(result.CatalogFilesParsingReport);

            Assert.IsNotNull(result.UsageFilesParsingReport);
            Assert.IsTrue(result.UsageFilesParsingReport.IsCompletedSuccessfuly);
            Assert.IsFalse(result.UsageFilesParsingReport.HasErrors);
            Assert.IsNull(result.ModelMetrics);
            Assert.IsNull(result.EvaluationFilesParsingReport);
        }

        [TestMethod]
        public void TrainSmallModelEnablingColdItemPlacementTest()
        {
            const string baseFolder = nameof(TrainSmallModelEnablingColdItemPlacementTest);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator();
            string catalogFilePath = Path.Combine(baseFolder, "catalog.csv");
            generator.CreateCatalogFile(catalogFilePath);
            string usageFileFolderPath = Path.Combine(baseFolder, "usage");
            Directory.CreateDirectory(usageFileFolderPath);
            generator.CreateUsageFile(Path.Combine(usageFileFolderPath, "usage.csv"), 30000);

            var parameters = ModelTrainingParameters.Default;
            parameters.EnableColdItemPlacement = true;
            parameters.EnableColdToColdRecommendations = true;
            
            var trainer = new ModelTrainer();
            ModelTrainResult result = trainer.TrainModel(parameters, usageFileFolderPath, catalogFilePath, null,
                CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompletedSuccessfuly);

            Assert.IsNotNull(result.CatalogFilesParsingReport);
            Assert.IsTrue(result.CatalogFilesParsingReport.IsCompletedSuccessfuly);
            Assert.IsFalse(result.CatalogFilesParsingReport.HasErrors);

            Assert.IsNotNull(result.UsageFilesParsingReport);
            Assert.IsTrue(result.UsageFilesParsingReport.IsCompletedSuccessfuly);
            Assert.IsFalse(result.UsageFilesParsingReport.HasErrors);
            Assert.IsNull(result.ModelMetrics);
            Assert.IsNull(result.EvaluationFilesParsingReport);
        }

        [TestMethod]
        public void TrainSmallModelEnablingUserToItemRecommendationsTest()
        {
            const string baseFolder = nameof(TrainSmallModelEnablingUserToItemRecommendationsTest);
            Directory.CreateDirectory(baseFolder);

            int usersCount = 50;
            int usageEventsCount = 30000;
            var generator = new ModelTrainingFilesGenerator(usersCount);
            string catalogFilePath = Path.Combine(baseFolder, "catalog.csv");
            generator.CreateCatalogFile(catalogFilePath);
            string usageFileFolderPath = Path.Combine(baseFolder, "usage");
            Directory.CreateDirectory(usageFileFolderPath);
            generator.CreateUsageFile(Path.Combine(usageFileFolderPath, "usage.csv"), usageEventsCount);

            var parameters = ModelTrainingParameters.Default;
            parameters.EnableUserToItemRecommendations = true;

            int itemsCount = 0;
            var users = new HashSet<string>();
            IDocumentStore documentStore = Substitute.For<IDocumentStore>();
            documentStore.AddDocumentsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Document>>(),
                    Arg.Any<CancellationToken>())
                .Returns(info =>
                {
                    var docs = info.Arg<IEnumerable<Document>>().ToList();
                    foreach (Document document in docs)
                    {
                        users.Add(document.Id);
                        itemsCount += document.Content?.Split(',').Length ?? 0;
                    }
                    
                    return Task.FromResult(docs.Count);
                });
            
            var trainer = new ModelTrainer(documentStore: documentStore);
            ModelTrainResult result = trainer.TrainModel(parameters, usageFileFolderPath, catalogFilePath, null,
                CancellationToken.None);

            // expect only one call to document store
            documentStore.ReceivedWithAnyArgs(1);

            // make sure that all the users got their history stored
            Assert.AreEqual(usersCount, users.Count);

            // make sure the amount of stored history doesn't exceeds 100 items per user
            Assert.IsTrue(itemsCount <= usersCount * 100);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompletedSuccessfuly);

            Assert.IsNotNull(result.CatalogFilesParsingReport);
            Assert.IsTrue(result.CatalogFilesParsingReport.IsCompletedSuccessfuly);
            Assert.IsFalse(result.CatalogFilesParsingReport.HasErrors);

            Assert.IsNotNull(result.UsageFilesParsingReport);
            Assert.IsTrue(result.UsageFilesParsingReport.IsCompletedSuccessfuly);
            Assert.IsFalse(result.UsageFilesParsingReport.HasErrors);
            Assert.IsNull(result.ModelMetrics);
            Assert.IsNull(result.EvaluationFilesParsingReport);
        }

        [TestMethod]
        public void ModelEvaluationTest()
        {
            const string baseFolder = nameof(ModelEvaluationTest);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator(20,50);
            string usageFileFolderPath = Path.Combine(baseFolder, "usage");
            Directory.CreateDirectory(usageFileFolderPath);
            string evaluationFileFolderPath = Path.Combine(baseFolder, "evaluation");
            Directory.CreateDirectory(evaluationFileFolderPath);
            generator.CreateEvaluationFiles(Path.Combine(usageFileFolderPath, "usage.csv"), Path.Combine(evaluationFileFolderPath, "evaluationUsage.csv"), 500, 30);

            var trainer = new ModelTrainer();

            var modelTrainingParameters = ModelTrainingParameters.Default;
            ModelTrainResult result = trainer.TrainModel(modelTrainingParameters, usageFileFolderPath, null, evaluationFileFolderPath, CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompletedSuccessfuly);
            Assert.IsNull(result.CatalogFilesParsingReport);

            Assert.IsNotNull(result.UsageFilesParsingReport);
            Assert.IsTrue(result.UsageFilesParsingReport.IsCompletedSuccessfuly);
            Assert.IsFalse(result.UsageFilesParsingReport.HasErrors);
            Assert.IsFalse(result.EvaluationFilesParsingReport.HasErrors);
            Assert.IsNotNull(result.ModelMetrics);
            Assert.IsNotNull(result.ModelMetrics.ModelDiversityMetrics);
            Assert.IsNotNull(result.ModelMetrics.ModelPrecisionMetrics);
        }
    }
}
