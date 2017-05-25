// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Recommendations.UnitTest.Core
{
    public class ModelTrainingFilesGenerator
    {
        /// <summary>
        /// Gets the generated user ids
        /// </summary>
        public IList<string> Users => _users.ToArray();

        /// <summary>
        /// Creates a new instance of the <see cref="ModelTrainingFilesGenerator"/> class.
        /// </summary>
        /// <param name="usersCount">The number of users in the population</param>
        /// <param name="itemsCount">The number of possible events</param>
        /// <param name="timestampsRange">The range of possible timestamps</param>
        /// <param name="featuresCount">The number of possible features</param>
        /// <param name="featureValuesCount">The number of possible feature values</param>
        public ModelTrainingFilesGenerator(
            int usersCount = 100, 
            int itemsCount = 10, 
            TimeSpan? timestampsRange = null,
            int featuresCount = 3,
            int featureValuesCount = 2)
        {
            _random = new Random();
            _users = Enumerable.Range(1, usersCount).Select(i => $"user_{i}").ToList();
            _items = Enumerable.Range(1, itemsCount).Select(i => $"item_{i}").ToList();
            _timestampRange = timestampsRange ?? TimeSpan.FromDays(30);
            _features = Enumerable.Range(1, featuresCount).Select(i => $"feature_{i}").ToList();
            _featureValues = Enumerable.Range(1, featureValuesCount).Select(i => $"value_{i}").ToList();
        }

        /// <summary>
        /// Creates a catalog CSV file.
        /// </summary>
        /// <param name="filePath">The path to the file to create</param>
        /// <param name="addFeatures">Indicates whether to add item features</param>
        public IList<string> CreateCatalogFile(string filePath, bool addFeatures = true)
        {
            IEnumerable<string> catalogItems = _items.Select(itemId => $"{itemId},{itemId},category,description");
            if (addFeatures)
            {
                string features = string.Join(",", _features.Where(_ => _random.NextDouble() > 0.9)
                    .Select(name => $"{name}={_featureValues[_random.Next(_featureValues.Count)]}"));
                catalogItems = catalogItems.Select(item => $"{item},{features}");
            }

            File.WriteAllLines(filePath, catalogItems);
            return _items.ToList();
        }

        /// <summary>
        /// Creates a usage event CSV file.
        /// </summary>
        /// <param name="filePath">The path to the file to create</param>
        /// <param name="usageEventsCount">The number of usage events to create</param>
        /// <returns>The items that appear in the generated usage file ('warm' items)</returns>
        public IList<string> CreateUsageFile(string filePath, int usageEventsCount)
        {
            _timestampBase = DateTime.UtcNow;
            var itemsUsed = new HashSet<string>();
            File.WriteAllLines(filePath, Enumerable.Repeat(0, usageEventsCount).Select(_ => CreateUsageEvent(itemsUsed)));
            return itemsUsed.ToList();
        }

        /// <summary>
        /// Creates a usage event CSV file and corresponding evaluation event CSV file.
        /// </summary>
        /// <param name="usageFilePath">The path to the usage file to create</param>
        /// <param name="evaluationFilePath">The path to the evaluation file to create</param>
        /// <param name="usageEventsCount">The number of usage events to create</param>
        /// <param name="testPercentage">Percentage of usage events to move to test</param>
        /// <returns>The items that appear in the generated usage file ('warm' items)</returns>
        public IList<string> CreateEvaluationFiles(string usageFilePath, string evaluationFilePath, int usageEventsCount, int testPercentage)
        {
            _timestampBase = DateTime.UtcNow;
            var itemsUsed = new HashSet<string>();

            var events = Enumerable.Repeat(0, usageEventsCount).Select(_ => CreateUsageEvent(itemsUsed)).ToList();

            int testEventCount = events.Count*(100 - testPercentage)/100;
            var usageEvents = events.Take(testEventCount);
            var evaluationEvents = events.Skip(testEventCount);

            File.WriteAllLines(usageFilePath, usageEvents);
            File.WriteAllLines(evaluationFilePath, evaluationEvents);
            return itemsUsed.ToList();
        }

        private string CreateUsageEvent(HashSet<string> itemsUsed)
        {
            string itemId = _items[_random.Next(_items.Count)];
            itemsUsed.Add(itemId);
            string userId = _users[_random.Next(_users.Count)];
            DateTime timestamp = _timestampBase.AddSeconds(_random.Next((int)_timestampRange.TotalSeconds));
            return $"{userId},{itemId},{timestamp}";
        }

        private readonly Random _random;
        public readonly IList<string> _users;
        private readonly IList<string> _items;
        private readonly IList<string> _features;
        private readonly IList<string> _featureValues;
        private DateTime _timestampBase;
        private readonly TimeSpan _timestampRange;
    }
}