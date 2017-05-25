// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Recommendations.Core;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// An implementation of <see cref="IDocumentStore"/> using an underlying Azure storage table
    /// </summary>
    public class AzureDocumentStore : IDocumentStore
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AzureCloudTable"/> class.
        /// </summary>
        /// <param name="cloudTable">The underlying Azure storage table</param>
        public AzureDocumentStore(CloudTable cloudTable)
        {
            if (cloudTable == null)
            {
                throw new ArgumentNullException(nameof(cloudTable));
            }

            _cloudTable = cloudTable;
        }

        /// <summary>
        /// Creates the document store is not already exists. 
        /// </summary>
        /// <returns><value>true</value> if the document store was created; Otherwise, <value>false</value></returns>
        public bool CreateIfNotExists()
        {
            return _cloudTable.CreateIfNotExists();
        }

        /// <summary>
        /// Gets a document from the store
        /// </summary>
        /// <param name="key">The document partition key</param>
        /// <param name="id">The id of the document</param>
        /// <returns>All the documents associated with the key</returns>
        public Document GetDocument(string key, string id)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            // create an entity retrieval operation
            TableOperation retrieveOperation =
                TableOperation.Retrieve<DynamicTableEntity>(key, id, new List<string> { nameof(Document.Content) });
            
            try
            {
                // query for all of the entities
                Trace.TraceVerbose($"Getting a document entity under partition key '{key}' and row key '{id}' from the table");
                var entity = _cloudTable.Execute(retrieveOperation).Result as DynamicTableEntity;
                return ConvertEntityToDocument(entity);
            }
            catch (StorageException storageException)
            {
                // check if failed because the table doesn't exists
                if (storageException.RequestInformation?.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    Trace.TraceWarning($"Storage table {_cloudTable.Name} doesn't exists");
                    return null;
                }

                var exception = new Exception(
                    $"Exception while trying to get all document entities under partition key '{key}' from the table", storageException);
                Trace.TraceError(exception.ToString());
                throw exception;
            }
        }

        /// <summary>
        /// Adds documents to the storage
        /// </summary>
        /// <param name="key">The key to add the documents under</param>
        /// <param name="documents">The documents to insert</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns>The number of successfully inserted items</returns>
        public async Task<int> AddDocumentsAsync(string key, IEnumerable<Document> documents, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            // create a batch operation for inserting items in batches of 100
            int insertedItemsCount = 0;
            var batchOperation = new TableBatchOperation();
            foreach (Document document in documents)
            {
                batchOperation.InsertOrReplace(new DynamicTableEntity(key, document.Id)
                {
                    Properties = {{nameof(Document.Content), new EntityProperty(document.Content)}}
                });

                if (batchOperation.Count == 100)
                {
                    Trace.TraceVerbose($"Batch inserting {batchOperation.Count} document entities under partition key '{key}'");
                    IList<TableResult> results = await _cloudTable.ExecuteBatchAsync(batchOperation, cancellationToken);

                    // a successful InsertOrReplace operation returns 204 (No Content)
                    insertedItemsCount = results.Count(result => result.HttpStatusCode == (int)HttpStatusCode.NoContent);

                    // create a new batch operation
                    batchOperation = new TableBatchOperation();
                }
            }

            if (batchOperation.Count > 0)
            {
                Trace.TraceVerbose($"Batch inserting {batchOperation.Count} document entities under partition key '{key}'");
                IList<TableResult> results = await _cloudTable.ExecuteBatchAsync(batchOperation, cancellationToken);

                // a successful InsertOrReplace operation returns 204 (No Content)
                insertedItemsCount = results.Count(result => result.HttpStatusCode == (int)HttpStatusCode.NoContent);
            }

            Trace.TraceVerbose($"Successfully inserted {insertedItemsCount} items under partition key '{key}'");
            return insertedItemsCount;
        }
        
        /// <summary>
        /// Deletes the document store is exists. 
        /// </summary>
        /// <returns><value>true</value> if the document store was deleted; Otherwise, <value>false</value></returns>
        public Task<bool> DeleteIfExistsAsync()
        {
            return _cloudTable.DeleteIfExistsAsync();
        }

        private Document ConvertEntityToDocument(DynamicTableEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new Document
            {
                Id = entity.RowKey,
                Content = entity.Properties[nameof(Document.Content)]?.StringValue
            };
        }
        
        private readonly CloudTable _cloudTable;
        private static readonly ITracer Trace = new Tracer(nameof(AzureDocumentStore));
    }
}