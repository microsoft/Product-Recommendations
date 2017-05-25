// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Recommendations.Core.Sar;

namespace Recommendations.Core.Train
{
    /// <summary>
    /// A class for storing and retrieving the history of usage events per user
    /// </summary>
    internal class UserHistoryStore
    {
        /// <summary>
        /// Creates a new instance of the <see cref="UserHistoryStore"/> class.
        /// </summary>
        /// <param name="documentStore">The underlying document store</param>
        /// <param name="tracer">A message tracer to use for logging</param>
        /// <param name="progressMessageReportDelegate">A delegate for reporting progress messages</param>
        public UserHistoryStore(IDocumentStore documentStore, ITracer tracer, Action<string> progressMessageReportDelegate)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }
            
            _documentStore = documentStore;
            _progressMessageReportDelegate = progressMessageReportDelegate ?? (_ => { });
            _tracer = tracer ?? new DefaultTracer();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="UserHistoryStore"/> class.
        /// </summary>
        /// <param name="documentStore">The underlying document store</param>
        /// <param name="usersCount">The total number of users</param>
        /// <param name="tracer">A message tracer to use for logging</param>
        public UserHistoryStore(IDocumentStore documentStore, int usersCount, ITracer tracer)
        {
            if (documentStore == null)
            {
                throw new ArgumentNullException(nameof(documentStore));
            }

            if (usersCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usersCount));
            }

            _usersCount = usersCount;
            _documentStore = documentStore;
            _progressMessageReportDelegate = _ => { };
            _tracer = tracer ?? new DefaultTracer();
        }

        /// <summary>
        /// gets all the usage events associated with a user
        /// </summary>
        /// <param name="userId">The id of the user to retrieve the history for</param>
        /// <returns>All the usage events associated with the user</returns>
        public IList<SarUsageEvent> GetUserHistory(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            int partitionsHashFactor = GetPartitionHashFactor();
            int partitionKey = userId.GetHashCode() % partitionsHashFactor;

            _tracer.TraceVerbose($"Reading stored document with id '{userId}' and partition key '{partitionKey}'");
            Document document = _documentStore.GetDocument(partitionKey.ToString(), userId);

            // deserialize the document's content into usage events
            List<SarUsageEvent> userHistory = DeserializeUsageEvents(document?.Content)?.ToList();

            _tracer.TraceVerbose($"Found {userHistory?.Count} user history usage events for user '{userId}'");
            return userHistory ?? new List<SarUsageEvent>();
        }

        /// <summary>
        /// Stores input usage events to the usage history store
        /// </summary>
        /// <param name="usageEvents">The usage events to store</param>
        /// <param name="userIdsIndex">A user id index</param>
        /// <param name="cancellationToken">A cancellation token used to abort the training</param>
        public Task StoreUserHistoryEventsAsync(IEnumerable<SarUsageEvent> usageEvents, string[] userIdsIndex, CancellationToken cancellationToken)
        {
            if (usageEvents == null)
            {
                throw new ArgumentNullException(nameof(usageEvents));
            }

            if (userIdsIndex == null)
            {
                throw new ArgumentNullException(nameof(userIdsIndex));
            }

            _storedDocumentsCount = 0;
            _storedDocumentsReportInterval = userIdsIndex.Length >= 100 ? userIdsIndex.Length / 100 : userIdsIndex.Length;
            _usersCount = userIdsIndex.Length;

            // start uploading the events in batches
            return BatchUploadUserHistoryEventsAsync(
                usageEvents.Where(e => e.UserId > 0 && e.UserId <= userIdsIndex.Length),
                userIdsIndex, cancellationToken);
        }
        
        /// <summary>
        /// Uploads the usage events in batches
        /// </summary>
        private async Task BatchUploadUserHistoryEventsAsync(IEnumerable<SarUsageEvent> usageEvents, string[] userIdsIndex, CancellationToken cancellationToken)
        {
            int partitionsHashFactor = GetPartitionHashFactor();

            // group the usage events into partitions
            List<IGrouping<int, SarUsageEvent>> partitions =
                usageEvents.GroupBy(usageEvent => userIdsIndex[usageEvent.UserId - 1].GetHashCode() % partitionsHashFactor).ToList();

            List<Task> tasks = new List<Task>();
            foreach (IGrouping<int, SarUsageEvent> usageEventPartition in partitions)
            {
                // start uploading the partition
                tasks.Add(UploadPartitionAsync(usageEventPartition.Key.ToString(), usageEventPartition, userIdsIndex, cancellationToken));

                // if the maximum open connections was reached, wait for at least one to complete
                if (tasks.Count >= MaxConnectionsCount)
                {
                    await Task.WhenAny(tasks);
                    tasks = tasks.Where(task => !task.IsCompleted).ToList();
                }
            }

            // wait for the remainder of the connections to complete
            await Task.WhenAll(tasks.Where(task => !task.IsCompleted));
        }

        /// <summary>
        /// Uploads the latest 100 usage events for each user to a specific partition
        /// </summary>
        private async Task UploadPartitionAsync(string partitionKey, IEnumerable<SarUsageEvent> usageEvents, string[] userIdsIndex, CancellationToken cancellationToken)
        {
            // group the usage events by user id
            IList<Document> documents =
                usageEvents.GroupBy(usageEvent => usageEvent.UserId)
                    .Select(group => new Document
                    {
                        Id = userIdsIndex[group.Key - 1],

                        // serialize only the user's latest 100 usage events
                        Content = SerializeUsageEvents(group.OrderByDescending(u => u.TimestampAsDateTime).Take(100))
                    })
                    .ToArray();

            _tracer.TraceVerbose($"Storing {documents.Count} documents in partition '{partitionKey}'");

            // add user usage events to document store 
            int documentsAdded = await _documentStore.AddDocumentsAsync(partitionKey, documents, cancellationToken);
            _tracer.TraceVerbose($"Stored {documentsAdded} documents to partition '{partitionKey}'");

            lock (this)
            {
                // update the stored documents counter
                _storedDocumentsCount += documentsAdded;
            }

            // report the progress if reached the interval
            if (_storedDocumentsCount % _storedDocumentsReportInterval == 0)
            {
                double progress = (float)_storedDocumentsCount / userIdsIndex.Length;
                _progressMessageReportDelegate($"Storing User History: {progress:P1}");
            }
        }

        /// <summary>
        /// Get the factor to use to partition user ids
        /// </summary>
        /// <returns></returns>
        private int GetPartitionHashFactor()
        {
            return _usersCount < 100 ? 1 : _usersCount / 100;
        }

        /// <summary>
        /// Serialize a list of <see cref="SarUsageEvent"/> instances into a string
        /// </summary>
        private static string SerializeUsageEvents(IEnumerable<SarUsageEvent> usageEvents)
        {
            return string.Join(",", usageEvents.Select(e =>
                $"{e.ItemId:X}.{e.Timestamp.Ticks.RawValue:X}.{BitConverter.ToUInt32(BitConverter.GetBytes(e.Weight), 0):X}"));
        }

        /// <summary>
        /// Deserialize a string of serialized list of usage events into a list of <see cref="SarUsageEvent"/> instances
        /// </summary>
        private static IEnumerable<SarUsageEvent> DeserializeUsageEvents(string serializedUsageEvents)
        {
            return serializedUsageEvents?.Split(',')
                .Select(serializedUsageEvent => serializedUsageEvent?.Split('.'))
                .Where(usageEventParts => usageEventParts?.Length == 3)
                .Select(usageEventParts => new SarUsageEvent
                {
                    ItemId = uint.Parse(usageEventParts[0], NumberStyles.HexNumber),
                    TimestampAsDateTime = DateTime.FromBinary(long.Parse(usageEventParts[1], NumberStyles.HexNumber)),
                    Weight = BitConverter.ToSingle(
                        BitConverter.GetBytes(uint.Parse(usageEventParts[2], NumberStyles.HexNumber)), 0)
                });
        }
        
        private int _storedDocumentsCount;
        private int _storedDocumentsReportInterval;
        private int _usersCount;
        private readonly IDocumentStore _documentStore;
        private readonly ITracer _tracer;
        private readonly Action<string> _progressMessageReportDelegate;

        private const uint MaxConnectionsCount = 500;
    }
}
