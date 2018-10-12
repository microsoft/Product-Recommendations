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
    internal class CatalogFileParser
    {
        /// <summary>
        /// Gets the item ids index map
        /// </summary>
        public ConcurrentDictionary<string, uint> ItemIdsIndex { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="CatalogFileParser"/> class.
        /// </summary>
        /// <param name="maximumParsingErrorsCount">The maximal number of line parsing errors to tolerate</param>
        /// <param name="itemIdsIndex">An item ids index map</param>
        /// <param name="tracer">A message tracer to use for logging</param>
        public CatalogFileParser(uint maximumParsingErrorsCount = 0, ConcurrentDictionary<string, uint> itemIdsIndex = null, ITracer tracer = null)
        {
            _maximumParsingErrorsCount = maximumParsingErrorsCount;
            ItemIdsIndex = itemIdsIndex ?? new ConcurrentDictionary<string, uint>();
            _tracer = tracer ?? new DefaultTracer();
        }

        /// <summary>
        /// Parse a catalog file to <see cref="SarCatalogItem"/> items.
        /// </summary>
        /// <param name="catalogFilePath">The file to parse</param>
        /// <param name="cancellationToken">A cancellation token used to abort the operation</param>
        /// <param name="catalogItems">The parsed catalog items</param>
        /// <param name="featureNames">The parsed names of the catalog items features, in the same order as the feature values in the catalog</param>
        /// <returns>The parsing report</returns>
        public FileParsingReport ParseCatalogFile(
            string catalogFilePath,
            CancellationToken cancellationToken,
            out IList<SarCatalogItem> catalogItems,
            out string[] featureNames)
        {
            if (string.IsNullOrWhiteSpace(catalogFilePath))
            {
                throw new ArgumentNullException(nameof(catalogFilePath));
            }

            if (!File.Exists(catalogFilePath))
            {
                throw new ArgumentException($"Failed to find catalog file under '{catalogFilePath}'", nameof(catalogFilePath));
            }

            _tracer.TraceInformation("Starting catalog file parsing");

            // parse the catalog file into catalog items
            var featureNamesIndex = new ConcurrentDictionary<string, uint>();
            var parsingReport = new FileParsingReport();
            IList<SarCatalogItem> parsedCatalogItems = ParseCatalogFile(catalogFilePath, featureNamesIndex, parsingReport).ToList();
            if (!parsingReport.IsCompletedSuccessfuly)
            {
                catalogItems = new List<SarCatalogItem>(0);
                featureNames = new string[0];
                _tracer.TraceError("Failed parsing catalog file");
                return parsingReport;
            }

            cancellationToken.ThrowIfCancellationRequested();
            
            // clear the feature index as it is no longer needed
            featureNames = featureNamesIndex.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
            featureNamesIndex.Clear();

            // inflate feature vectors
            int numberOfFeatures = featureNames.Length;
            catalogItems = parsedCatalogItems.Select(item => InflateFeaturesVector(item, numberOfFeatures)).ToList();

            _tracer.TraceInformation("Finished catalog file parsing");
            return parsingReport;
        }

        /// <summary>
        /// Inflates the catalog item's feature vector size to the required size.
        /// </summary>
        private SarCatalogItem InflateFeaturesVector(SarCatalogItem catalogItem, int newSize)
        {
            string[] newVector = new string[newSize];
            if (catalogItem.FeatureVector != null)
            {
                Array.Copy(catalogItem.FeatureVector, newVector, catalogItem.FeatureVector.Length);
            }

            catalogItem.FeatureVector = newVector;
            return catalogItem;
        }

        /// <summary>
        /// Parse the input catalog file into catalog items.
        /// </summary>
        private IEnumerable<SarCatalogItem> ParseCatalogFile(
            string catalogFilePath,
            ConcurrentDictionary<string, uint> featureNamesIndex,
            FileParsingReport parsingReport)
        {
            using (var reader = new TextFieldParser(catalogFilePath) { Delimiters = new[] { "," } })
            {
                while (!reader.EndOfData)
                {
                    string[] fields;
                    try
                    {
                        parsingReport.TotalLinesCount++;
                        fields = reader.ReadFields();
                    }
                    catch (MalformedLineException)
                    {
                        parsingReport.Errors.Add(
                            new ParsingError(Path.GetFileName(catalogFilePath), reader.ErrorLineNumber,
                                ParsingErrorReason.MalformedLine));
                        if (parsingReport.Errors.Count > _maximumParsingErrorsCount)
                        {
                            parsingReport.IsCompletedSuccessfuly = false;
                            yield break;
                        }

                        continue;
                    }

                    ParsingErrorReason? parsingError;
                    ParsingErrorReason? parsingWarning;
                    SarCatalogItem catalogItem = ParseCatalogItem(fields, featureNamesIndex, out parsingError, out parsingWarning);
                    if (parsingError.HasValue)
                    {
                        parsingReport.Errors.Add(
                            new ParsingError(Path.GetFileName(catalogFilePath), reader.LineNumber - 1, parsingError.Value));
                        if (parsingReport.Errors.Count > _maximumParsingErrorsCount)
                        {
                            parsingReport.IsCompletedSuccessfuly = false;
                            yield break;
                        }
                        
                        continue;
                    }

                    if (parsingWarning.HasValue)
                    {
                        parsingReport.Warnings.Add(
                            new ParsingError(Path.GetFileName(catalogFilePath), reader.LineNumber - 1, parsingWarning.Value));

                        continue;
                    }

                    parsingReport.SuccessfulLinesCount++;
                    yield return catalogItem;
                }
            }

            // no more lines to parse - mark the parsing as successful
            parsingReport.IsCompletedSuccessfuly = true;
        }

        /// <summary>
        /// Parse input fields into a single catalog item.
        /// </summary>
        private SarCatalogItem ParseCatalogItem(string[] fields, ConcurrentDictionary<string, uint> featureNamesIndex, out ParsingErrorReason? parsingError, out ParsingErrorReason? parsingWarning)
        {
            parsingError = null;
            parsingWarning = null;
            if (fields == null || fields.Length < 2 ||
                string.IsNullOrWhiteSpace(fields[0]) || string.IsNullOrWhiteSpace(fields[1]))
            {
                parsingError = ParsingErrorReason.MissingFields;
                return null;
            }

            // get the item id
            string itemId = fields[0].ToLowerInvariant();
            
            // check the length of the item id
            if (itemId.Length > UsageEventsFilesParser.ItemIdMaximalLength)
            {
                parsingError = ParsingErrorReason.ItemIdTooLong;
                return null;
            }

            // check for duplicate item id
            if(ItemIdsIndex.ContainsKey(itemId))
            {
                parsingWarning = ParsingErrorReason.DuplicateItemId;
                return null;
            }

            // for backward compatibility with Cognitive Services' Recommendation service,
            // fields #1 (item name), #2 (category) and #3 (description) are ignored 

            // if available, parse features from field #5 forward
            string[] featureVector = null;
            if (fields.Length > 4)
            {
                // parse the item's features into a features vector
                featureVector = ParseCatalogItemFeatures(fields.Skip(4), featureNamesIndex);
                if (featureVector  == null)
                {
                    parsingError = ParsingErrorReason.MalformedCatalogItemFeature;
                    return null;
                }
            }

            // create a new catalog item
            return new SarCatalogItem
            {
                ItemId = ItemIdsIndex.GetOrAdd(itemId, key => (uint) ItemIdsIndex.Count + 1),
                FeatureVector = featureVector
            };
        }

        /// <summary>
        /// Parse catalog items features strings into a vector of feature values.
        /// Expected format: "name1=value1,name2=value2,...".
        /// Using the input feature name index, every feature name is assigned a numeric index 
        /// to be used as its index in the output vector. The content of each vector item is that feature value(s).
        /// </summary>
        private string[] ParseCatalogItemFeatures(IEnumerable<string> features, ConcurrentDictionary<string, uint> featureNamesIndex)
        {
            uint? maxIndexFound = null;
            var featureValuesMap = new Dictionary<uint, List<string>>();
            foreach (string featureString in features.Where(str => !string.IsNullOrWhiteSpace(str)))
            {
                // parse the feature string into name and value
                string featureName, featureValue;
                if (!TryParseFeatureString(featureString, out featureName, out featureValue))
                {
                    return null;
                }
                
                // find\create the feature's index
                uint index = featureNamesIndex.GetOrAdd(featureName, key => (uint)featureNamesIndex.Count);
                if (!maxIndexFound.HasValue || maxIndexFound < index)
                {
                    maxIndexFound = index;
                }

                // add the feature value to the feature values map
                List<string> featureValues;
                if (!featureValuesMap.TryGetValue(index, out featureValues))
                {
                    featureValues = new List<string>();
                    featureValuesMap.Add(index, featureValues);
                }

                featureValues.Add(featureValue);
            }
            
            // create a features vector
            var featuresVector = new string[maxIndexFound + 1 ?? 0];
            foreach (var feature in featureValuesMap)
            {
                featuresVector[feature.Key] = string.Join(";", feature.Value);
            }

            return featuresVector;
        }

        /// <summary>
        /// Parse a single feature string. Expected format is 'name=value'.
        /// </summary>
        /// <returns></returns>
        private bool TryParseFeatureString(string featureString, out string featureName, out string featureValue)
        {
            featureName = featureValue = null;
            string[] nameAndValue = (featureString ?? string.Empty).Split('=');
            if (nameAndValue.Length != 2 || string.IsNullOrWhiteSpace(nameAndValue[0]))
            {
                return false;
            }

            featureName = nameAndValue[0].Trim().ToLowerInvariant();
            featureValue = nameAndValue[1].Trim().ToLowerInvariant();
            return true;
        }

        private readonly uint _maximumParsingErrorsCount;
        private readonly ITracer _tracer;
    }
}
