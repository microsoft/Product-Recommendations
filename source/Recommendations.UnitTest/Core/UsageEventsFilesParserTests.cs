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
    public class UsageEventsFilesParserTests
    {
        [TestMethod]
        public void ParseTwoValidUsageFilesTest()
        {
            const string baseFolder = nameof(ParseTwoValidUsageFilesTest);
            Directory.CreateDirectory(baseFolder);

            var generator = new ModelTrainingFilesGenerator();
            generator.CreateUsageFile(Path.Combine(baseFolder, "usage1.csv"), 100);
            generator.CreateUsageFile(Path.Combine(baseFolder, "usage2.csv"), 50);

            IList<SarUsageEvent> usageEvents;
            var parser = new UsageEventsFilesParser();
            FileParsingReport report = parser.ParseUsageEventFiles(baseFolder, CancellationToken.None, out usageEvents);

            Assert.IsNotNull(report);
            Assert.IsTrue(report.IsCompletedSuccessfuly);
            Assert.AreEqual(150, report.SuccessfulLinesCount);
            Assert.AreEqual(150, report.TotalLinesCount);
            Assert.IsTrue(report.Errors == null || !report.Errors.Any());
        }
    }
}
