// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Recommendations.Common.Api;
using Recommendations.Core.Parsing;
using Recommendations.WebJob;

namespace Recommendations.UnitTest.WebJob
{
    [TestClass]
    public class WebJobLogicTests
    {
        [TestMethod]
        public void CreateParsingReportTest()
        {

            FileParsingReport fileParsingReport = new FileParsingReport {SuccessfulLinesCount = 10};

            // Add errors
            fileParsingReport.Errors.Add(new ParsingError(null, 10, ParsingErrorReason.BadTimestampFormat));
            fileParsingReport.Errors.Add(new ParsingError(null, 11, ParsingErrorReason.BadTimestampFormat));

            fileParsingReport.Errors.Add(new ParsingError(null, 12, ParsingErrorReason.BadWeightFormat));

            // Add warnings
            fileParsingReport.Warnings.Add(new ParsingError(null, 14, ParsingErrorReason.DuplicateItemId));
            fileParsingReport.Warnings.Add(new ParsingError(null, 15, ParsingErrorReason.DuplicateItemId));

            ParsingReport parsingReport = WebJobLogic.CreateParsingReport(fileParsingReport, new TimeSpan(), null);
            Assert.IsNotNull(parsingReport);
            Assert.AreEqual(3, parsingReport.Errors.Count);

            // Ensure errors are added in parsing report
            Assert.IsTrue(
                parsingReport.Errors.Select(x => x.Error)
                    .Contains(ParsingErrorReason.BadTimestampFormat));
            Assert.AreEqual(1,
                parsingReport.Errors
                    .Count(x => x.Error == ParsingErrorReason.BadTimestampFormat));
            Assert.AreEqual(2,
                parsingReport.Errors
                    .First(x => x.Error == ParsingErrorReason.BadTimestampFormat)
                    .Count);

            Assert.IsTrue(
                parsingReport.Errors.Select(x => x.Error)
                    .Contains(ParsingErrorReason.BadWeightFormat));
            Assert.AreEqual(1,
                parsingReport.Errors
                    .Count(x => x.Error == ParsingErrorReason.BadWeightFormat));
            Assert.AreEqual(1,
                parsingReport.Errors
                    .First(x => x.Error == ParsingErrorReason.BadWeightFormat)
                    .Count);

            // Ensure warnings are added in parsing report
            Assert.IsTrue(
                parsingReport.Errors.Select(x => x.Error)
                    .Contains(ParsingErrorReason.DuplicateItemId));
            Assert.AreEqual(1,
                parsingReport.Errors
                    .Count(x => x.Error == ParsingErrorReason.DuplicateItemId));
            Assert.AreEqual(2,
                parsingReport.Errors
                    .First(x => x.Error == ParsingErrorReason.DuplicateItemId)
                    .Count);
        }
    }
}
