// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Recommendations.Core.Parsing;
using Recommendations.Core.Sar;

namespace Recommendations.UnitTest.Core
{
    [TestClass]
    public class CatalogFileParserTests
    {
        [TestMethod]
        public void ParseAValidCatalogFileTest()
        {
            const int catalogItemsCount = 20;
            const string baseFolder = nameof(ParseAValidCatalogFileTest);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator(itemsCount: catalogItemsCount);
            string catalogFile = Path.Combine(baseFolder, "catalog.csv");
            generator.CreateCatalogFile(catalogFile);

            IList<SarCatalogItem> catalogItems;
            string[] featureNames;
            var parser = new CatalogFileParser();
            FileParsingReport report =
                parser.ParseCatalogFile(catalogFile, CancellationToken.None, out catalogItems, out featureNames);

            Assert.IsNotNull(report);
            Assert.IsTrue(report.IsCompletedSuccessfuly);
            Assert.AreEqual(catalogItemsCount, report.SuccessfulLinesCount);
            Assert.AreEqual(catalogItemsCount, report.TotalLinesCount);
            Assert.IsTrue(report.Errors == null || !report.Errors.Any());
        }
    }
}