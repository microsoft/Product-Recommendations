// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualBasic.FileIO;
using Recommendations.Core.Sar;

namespace Recommendations.Core.Parsing
{
    internal class UsageEventsFilesParser
    {
        /// <summary>
        /// Gets the default event weight used if a weight field is missing 
        /// </summary>
        public const float DefaultUsageEventWeight = 1.0f;

        /// <summary>
        /// Gets the item ids index map
        /// </summary>
        public ConcurrentDictionary<string, uint> ItemIdsIndex { get; }

        /// <summary>
        /// Gets the user ids index map
        /// </summary>
        public ConcurrentDictionary<string, uint> UserIdsIndex { get; }

        /// <summary>
        /// Gets the most recent timestamp in the parsed usage events
        /// </summary>
        public DateTime MostRecentEventTimestamp { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="UsageEventsFilesParser"/> class.
        /// </summary>
        /// <param name="itemIdsIndex">An initial item ids index map</param>
        /// <param name="userIdsIndex">An initial user ids index map</param>
        /// <param name="maximumParsingErrorsCount">The maximal number of line parsing errors to tolerate</param>
        /// <param name="ignoreUnknownItemIds">Indicates whether to ignore newly found item ids or tp parse them and add to the index</param>
        /// <param name="tracer">A message tracer to use for logging</param>
        public UsageEventsFilesParser(
            ConcurrentDictionary<string, uint> itemIdsIndex = null,
            ConcurrentDictionary<string, uint> userIdsIndex = null,
            uint maximumParsingErrorsCount = 0,
            bool ignoreUnknownItemIds = false,
            ITracer tracer = null)
        {
            _ignoreUnknownItemIds = ignoreUnknownItemIds;
            _maximumParsingErrorsCount = maximumParsingErrorsCount;
            ItemIdsIndex = itemIdsIndex ?? new ConcurrentDictionary<string, uint>();
            UserIdsIndex = userIdsIndex ?? new ConcurrentDictionary<string, uint>();
            _tracer = tracer ?? new DefaultTracer();
        }

        /// <summary>
        /// Parses each file found in the input folder into usage events, 
        /// while indexing the found user ids and item ids.
        /// </summary>
        /// <param name="usageFolder">A folder containing the usage files to parse</param>
        /// <param name="cancellationToken">A cancellation token used to abort the operation</param>
        /// <param name="usageEvents">The parsed usage events</param>
        /// <returns>The parsing report</returns>
        public FileParsingReport ParseUsageEventFiles(string usageFolder, CancellationToken cancellationToken, out IList<SarUsageEvent> usageEvents)
        {
            if (string.IsNullOrWhiteSpace(usageFolder))
            {
                throw new ArgumentNullException(nameof(usageFolder));
            }

            if (!Directory.Exists(usageFolder))
            {
                throw new ArgumentException($"Failed to find usage files folder: '{usageFolder}'", nameof(usageFolder));
            }

            _tracer.TraceInformation("Starting usage files parsing");

            var defaultEventTimestamp = DateTime.UtcNow;
            MostRecentEventTimestamp = DateTime.MinValue;
            var parsingReport = new FileParsingReport();
            usageEvents = ParseUsageEventFilesInternal(usageFolder, parsingReport, defaultEventTimestamp, cancellationToken).ToList();

            _tracer.TraceInformation("Finished usage files parsing");
            return parsingReport;
        }

        /// <summary>
        /// Parses each file found in the input folder into usage events, 
        /// while indexing the found user ids and item ids.
        /// </summary>
        private IEnumerable<SarUsageEvent> ParseUsageEventFilesInternal(string usageFolder, FileParsingReport parsingReport, DateTime defaultEventTimestamp, CancellationToken cancellationToken)
        {
            foreach (string usageFile in Directory.GetFiles(usageFolder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _tracer.TraceInformation($"Parsing file {Path.GetFileName(usageFile)} ({(double)new FileInfo(usageFile).Length/(1024*1024):F2} MB)");

                foreach (SarUsageEvent sarUsageEvent in ParseUsageEventsFile(usageFile, parsingReport, defaultEventTimestamp))
                {
                    yield return sarUsageEvent;
                }

                if (parsingReport.Errors.Count > _maximumParsingErrorsCount)
                {
                    parsingReport.IsCompletedSuccessfuly = false;
                    yield break;
                }
            }

            // no more lines to parse - mark the parsing as successful
            parsingReport.IsCompletedSuccessfuly = true;
        }

        /// <summary>
        /// Parse a usage events file into usage events items.
        /// Expected format is userId,itemId,timestamp[,event-type[,event-weight]
        /// </summary>
        private IEnumerable<SarUsageEvent> ParseUsageEventsFile(string usageFile, FileParsingReport parsingReport, DateTime defaultEventTimestamp)
        {
            using (var reader = new TextFieldParser(usageFile) { Delimiters = new[] { "," } })
            {
                while (!reader.EndOfData)
                {
                    string[] fields;
                    parsingReport.TotalLinesCount++;
                    
                    try
                    {
                        fields = reader.ReadFields();
                    }
                    catch (MalformedLineException ex)
                    {
                        if (ShouldContinueAfterError(ParsingErrorReason.MalformedLine, 
                            parsingReport, usageFile, ex.LineNumber))
                        {
                            continue;
                        }

                        yield break;
                    }

                    ParsingErrorReason? parsingError;
                    ParsingErrorReason? parsingWarning;
                    SarUsageEvent usageEvent = ParseUsageEvent(fields, defaultEventTimestamp, out parsingError, out parsingWarning);
                    if (parsingError.HasValue)
                    {
                        if (ShouldContinueAfterError(parsingError.Value,
                            parsingReport, usageFile, reader.LineNumber - 1))
                        {
                            continue;
                        }

                        yield break;
                    }

                    if (parsingWarning.HasValue)
                    {
                        parsingReport.Warnings.Add(new ParsingError(Path.GetFileName(usageFile), reader.LineNumber - 1,
                            parsingWarning.Value));

                        continue;
                    }

                    parsingReport.SuccessfulLinesCount++;
                    yield return usageEvent;
                }
            }
        }

        /// <summary>
        /// Parse a usage events file into usage events items.
        /// </summary>
        private SarUsageEvent ParseUsageEvent(string[] fields, DateTime defaultEventTimestamp, out ParsingErrorReason? parsingError, out ParsingErrorReason? parsingWarning)
        {
            parsingError = null;
            parsingWarning = null;
            if (fields == null || fields.Length < 2 ||
                string.IsNullOrWhiteSpace(fields[0]) || string.IsNullOrWhiteSpace(fields[1]))
            {
                parsingError = ParsingErrorReason.MissingFields;
                return null;
            }

            // parse the timestamp (if provided) or use the default value
            DateTime timestamp = defaultEventTimestamp;
            if (fields.Length > 2 && !DateTime.TryParse(fields[2], out timestamp))
            {
                parsingError = ParsingErrorReason.BadTimestampFormat;
                return null;
            }

            if (timestamp > MostRecentEventTimestamp)
            {
                MostRecentEventTimestamp = timestamp;
            }

            // get the item id
            string itemId = fields[1].ToLowerInvariant();

            // check the length of the user id
            if (itemId.Length > ItemIdMaximalLength)
            {
                parsingError = ParsingErrorReason.ItemIdTooLong;
                return null;
            }

            // check if to ignore the item
            if (_ignoreUnknownItemIds && !ItemIdsIndex.ContainsKey(itemId))
            {
                parsingWarning = ParsingErrorReason.UnknownItemId;
                return null;
            }

            // get the user id
            string userId = fields[0].ToLowerInvariant();

            // check the length of the user id
            if (userId.Length > UserIdMaximalLength)
            {
                parsingError = ParsingErrorReason.UserIdTooLong;
                return null;
            }

            // check for illegal characters in the user id
            if (!userId.All(IsAlphanumericDashOrUnderscoreCharacter))
            {
                parsingError = ParsingErrorReason.IllegalCharactersInUserId;
                return null;
            }

            // create a new usage event
            return new SarUsageEvent
            {
                Timestamp = timestamp,
                UserId = UserIdsIndex.GetOrAdd(userId, key => (uint)UserIdsIndex.Count + 1),
                ItemId = ItemIdsIndex.GetOrAdd(itemId, key => (uint)ItemIdsIndex.Count + 1)
            };
        }

        /// <summary>
        /// Records the error in the parsing report and determines if to continue with the parsing.
        /// </summary>
        private bool ShouldContinueAfterError(ParsingErrorReason error, FileParsingReport parsingReport, string usageFile, long lineNumber)
        {
            parsingReport.Errors.Add(new ParsingError(Path.GetFileName(usageFile), lineNumber, error));
            if (parsingReport.Errors.Count > _maximumParsingErrorsCount)
            {
                parsingReport.IsCompletedSuccessfuly = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the input character is alphanumeric, a dash or an underscore.
        /// </summary>
        internal static bool IsAlphanumericDashOrUnderscoreCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '-' || c == '_';
        }

        /// <summary>
        /// The maximal allowed length of item id strings
        /// </summary>
        internal const int ItemIdMaximalLength = 450;

        /// <summary>
        /// The maximal allowed length of user id strings
        /// </summary>
        internal const int UserIdMaximalLength = 255;

        private readonly uint _maximumParsingErrorsCount;
        private readonly bool _ignoreUnknownItemIds;
        private readonly ITracer _tracer;
    }
}
