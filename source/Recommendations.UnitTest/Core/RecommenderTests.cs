// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Recommendations.Common.Api;
using Recommendations.Core;
using Recommendations.Core.Recommend;
using Recommendations.Core.Train;
using Recommendations.WebApp.Models;

namespace Recommendations.UnitTest.Core
{
    [TestClass]
    [DeploymentItem("zlib.dll")]
    [DeploymentItem("CpuMathNative.dll")]
    public class RecommenderTests
    {
        [TestMethod]
        public void GetRecommendationsUsingSmallModelWithDefaultParameters()
        {
            const string baseFolder = nameof(GetRecommendationsUsingSmallModelWithDefaultParameters);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator(8);
            string usageFileFolderPath = Path.Combine(baseFolder, "usage");
            Directory.CreateDirectory(usageFileFolderPath);
            IList<string> warmItems = generator.CreateUsageFile(Path.Combine(usageFileFolderPath, "usage.csv"), 1000);

            var trainingParameters = ModelTrainingParameters.Default;
            trainingParameters.EnableBackfilling = false;
            var trainer = new ModelTrainer();
            ModelTrainResult result = trainer.TrainModel(trainingParameters, usageFileFolderPath, null, null, CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompletedSuccessfuly);

            var recommender = new Recommender(result.Model);
            var items = new List<UsageEvent>
            {
                new UsageEvent
                {
                    ItemId = warmItems.First(), 
                    EventType = UsageEventType.Click,
                    Timestamp = DateTime.UtcNow
                }
            };

            IList<Recommendation> recommendations = recommender.GetRecommendations(items, null, 3);
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Any());
            Assert.IsTrue(recommendations.All(r => r != null));
            Assert.IsTrue(recommendations.All(r => r.Score > 0 && !string.IsNullOrWhiteSpace(r.RecommendedItemId)));
        }

        [TestMethod]
        public void GetRecommendationsUsingUserId()
        {
            const string baseFolder = nameof(GetRecommendationsUsingUserId);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator(8);
            string usageFileFolderPath = Path.Combine(baseFolder, "usage");
            Directory.CreateDirectory(usageFileFolderPath);
            IList<string> warmItems = generator.CreateUsageFile(Path.Combine(usageFileFolderPath, "usage.csv"), 1000);
            
            var trainingParameters = ModelTrainingParameters.Default;
            trainingParameters.EnableBackfilling = false;
            trainingParameters.EnableUserToItemRecommendations = true;
            trainingParameters.AllowSeedItemsInRecommendations = true;

            Dictionary<string, Document> userHistory = null;
            IDocumentStore documentStore = Substitute.For<IDocumentStore>();
            documentStore.AddDocumentsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Document>>(),
                    Arg.Any<CancellationToken>())
                .Returns(info =>
                {
                    userHistory = info.Arg<IEnumerable<Document>>().ToDictionary(doc => doc.Id);
                    return Task.FromResult(userHistory.Count);
                });

            documentStore.GetDocument(Arg.Any<string>(), Arg.Any<string>())
                .Returns(info => userHistory?[info.ArgAt<string>(1)]);

            var trainer = new ModelTrainer(documentStore: documentStore);
            ModelTrainResult result = trainer.TrainModel(trainingParameters, usageFileFolderPath, null, null, CancellationToken.None);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompletedSuccessfuly);

            var recommender = new Recommender(result.Model, documentStore);
            var items = new List<UsageEvent>
            {
                new UsageEvent
                {
                    ItemId = warmItems.First(),
                    EventType = UsageEventType.Click,
                    Timestamp = DateTime.UtcNow
                }
            };

            string userId = generator.Users.FirstOrDefault();
            IList<Recommendation> recommendations = recommender.GetRecommendations(items, userId, 3);

            // expect the document store to be called once with the provided user id
            documentStore.Received(1).GetDocument(Arg.Any<string>(), userId);

            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Any());
            Assert.IsTrue(recommendations.All(r => r != null));
            Assert.IsTrue(recommendations.All(r => r.Score > 0 && !string.IsNullOrWhiteSpace(r.RecommendedItemId)));
        }
    }
}
