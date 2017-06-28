// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.MachineLearning.EntryPoints;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Recommendations.Common.Api;
using Recommendations.Core.Parsing;
using Recommendations.Core.Sar;
using Recommendations.Core.Train;

namespace Recommendations.UnitTest.Core
{
    [TestClass]
    [DeploymentItem("zlib.dll")]
    [DeploymentItem("CpuMathNative.dll")]
    public class SarTrainerTests
    {
        [TestMethod]
        public void TrainModelWithRangeOfPossibleParametersTest()
        {
            const string baseFolder = nameof(TrainModelWithRangeOfPossibleParametersTest);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator();

            // create catalog items
            IList<SarCatalogItem> catalogItems;
            string[] featureNames;
            string catalogFilePath = Path.Combine(baseFolder, "catalog.csv");
            generator.CreateCatalogFile(catalogFilePath);
            var itemIdsIndex = new ConcurrentDictionary<string, uint>();
            var catalogParser = new CatalogFileParser(0, itemIdsIndex);
            var parsingReport = catalogParser.ParseCatalogFile(catalogFilePath, CancellationToken.None, out catalogItems, out featureNames);
            Assert.IsTrue(parsingReport.IsCompletedSuccessfuly);
            
            // create usage items
            IList<SarUsageEvent> usageEvents;
            string usageFileFolderPath = Path.Combine(baseFolder, "usage");
            Directory.CreateDirectory(usageFileFolderPath);
            generator.CreateUsageFile(Path.Combine(usageFileFolderPath, "usage.csv"), 10000);
            var userIdsIndex = new ConcurrentDictionary<string, uint>();
            var usageFilesParser = new UsageEventsFilesParser(itemIdsIndex, userIdsIndex);
            parsingReport = usageFilesParser.ParseUsageEventFiles(usageFileFolderPath, CancellationToken.None, out usageEvents);
            Assert.IsTrue(parsingReport.IsCompletedSuccessfuly);
            
            int count = 0;
            var sarTrainer = new SarTrainer();
            IDictionary<string, double> catalogFeatureWeights;
            foreach (IModelTrainerSettings settings in GetAllModelTrainingParameters())
            {
                IPredictorModel model = sarTrainer.Train(settings, usageEvents, catalogItems, featureNames, userIdsIndex.Count, itemIdsIndex.Count, out catalogFeatureWeights);
                Assert.IsNotNull(model, $"Expected training to complete successfully when using settings#{count}: {settings}");
                count++;
            }
        }

        /// <summary>
        /// Returns an enumeration of <see cref="IModelTrainerSettings"/>, covering a range of possible valid settings. 
        /// </summary>
        private static IEnumerable<IModelTrainerSettings> GetAllModelTrainingParameters()
        {
            return
                from supportThreshold in new[] { 3, 6, 50 }
                from cooccurrenceUnit in Enum.GetValues(typeof(CooccurrenceUnit)).Cast<CooccurrenceUnit>()
                from similarityFunction in Enum.GetValues(typeof(SimilarityFunction)).Cast<SimilarityFunction>()
                from enableColdItemPlacement in new[] { false, true }
                from enableColdToColdRecommendations in new[] { false, true }.Select(v => v && enableColdItemPlacement).Distinct()
                from enableUserAffinity in new[] { false, true }
                from allowSeedItemsInRecommendations in new[] { false, true }
                select new ModelTrainingParameters
                {
                    SupportThreshold = supportThreshold,
                    CooccurrenceUnit = cooccurrenceUnit,
                    SimilarityFunction = similarityFunction,
                    EnableColdItemPlacement = enableColdItemPlacement,
                    EnableColdToColdRecommendations = enableColdToColdRecommendations,
                    EnableUserAffinity = enableUserAffinity,
                    AllowSeedItemsInRecommendations = allowSeedItemsInRecommendations
                };
        }
    }
}
