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
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using Recommendations.Core;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// An implementation of <see cref="ITable"/> using an underlying Azure storage table
    /// </summary>
    public class AzureCloudTable : ITable 
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AzureCloudTable"/> class.
        /// </summary>
        /// <param name="cloudTable">The underlying Azure storage table</param>
        public AzureCloudTable(CloudTable cloudTable)
        {
            if (cloudTable == null)
            {
                throw new ArgumentNullException(nameof(cloudTable));
            }

            _cloudTable = cloudTable;
        }

        /// <summary>
        /// Lists all the table entities
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <param name="selectColumns">Optional list of column names for projection.</param>
        /// <returns>A list of all the table entities in the partition</returns>
        public async Task<IList<TEntity>> ListEntitiesAsync<TEntity>(CancellationToken cancellationToken, params string[] selectColumns)
            where TEntity : TableEntity, new()
        {
            var entities = new List<TEntity>();
            TableContinuationToken token = null;

            // create a query for listing all entities
            string partitionKey = typeof(TEntity).Name;
            string partitionKeyMatch =
                TableQuery.GenerateFilterCondition(TableConstants.PartitionKey, QueryComparisons.Equal, partitionKey);
            TableQuery<TEntity> listEntitiesQuery = new TableQuery<TEntity>()
                .Where(partitionKeyMatch)
                .Select(selectColumns ?? new string[0]);

            Trace.TraceVerbose($"Listing all '{partitionKey}' entities in the table");

            do
            {
                // query for the next segment of table entities
                TableQuerySegment<TEntity> segment =
                    await _cloudTable.ExecuteQuerySegmentedAsync(listEntitiesQuery, token, cancellationToken);

                // update the continuation token
                token = segment.ContinuationToken;

                // add the found entities to the result list
                entities.AddRange(segment.Results);
            } while (token != null);

            Trace.TraceVerbose($"Found {entities.Count} '{partitionKey}' entities in the table");
            return entities;
        }

        /// <summary>
        /// Gets a table entity by id
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entityId">The id of the entity</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <param name="selectColumns">Optional list of column names for projection.</param>
        /// <returns>The retrieved entity of <value>null</value> if not found</returns>
        public async Task<TEntity> GetEntityAsync<TEntity>(string entityId, CancellationToken cancellationToken, params string[] selectColumns)
            where TEntity : TableEntity
        {
            if (entityId == null)
            {
                throw new ArgumentNullException(nameof(entityId));
            }
            
            // create a retrieve operation
            TableOperation retrieveOperation = TableOperation.Retrieve<TEntity>(typeof(TEntity).Name, entityId, selectColumns?.ToList());

            Trace.TraceVerbose($"Retrieving entity '{entityId}' from the table");

            // execute the operation
            TableResult result = await _cloudTable.ExecuteAsync(retrieveOperation, cancellationToken);

            Trace.TraceVerbose($"Done retrieving entity '{entityId}' from the table. StatusCode: {result.HttpStatusCode}");

            // return the entity
            return result.Result as TEntity;
        }

        /// <summary>
        /// Gets a table entity by id synchronously
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entityId">The id of the entity</param>
        /// <param name="selectColumns">Optional list of column names for projection.</param>
        /// <returns>The retrieved entity of <value>null</value> if not found</returns>
        public TEntity GetEntity<TEntity>(string entityId, params string[] selectColumns)
            where TEntity : TableEntity
        {
            if (entityId == null)
            {
                throw new ArgumentNullException(nameof(entityId));
            }

            // create a retrieve operation
            TableOperation retrieveOperation = TableOperation.Retrieve<TEntity>(typeof(TEntity).Name, entityId, selectColumns?.ToList());

            Trace.TraceVerbose($"Retrieving entity '{entityId}' from the table");

            // execute the operation
            TableResult result = _cloudTable.Execute(retrieveOperation);

            Trace.TraceVerbose($"Done retrieving entity '{entityId}' from the table. StatusCode: {result.HttpStatusCode}");

            // return the entity
            return result.Result as TEntity;
        }

        /// <summary>
        /// Inserts or replaces a table entity
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entity">The entity to insert\update</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was inserted\updated, <value>false</value> otherwise</returns>
        public async Task<bool> InsertOrReplaceEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            Trace.TraceVerbose($"Inserting or replacing entity '{entity.RowKey}' from the table");

            // insert or replace the entity
            TableResult result =
                await _cloudTable.ExecuteAsync(TableOperation.InsertOrReplace(entity), cancellationToken);

            // a successful InsertOrReplace operation returns 204 (No Content)
            Trace.TraceVerbose(
                $"Insert or replace entity '{entity.RowKey}' completed with status code: {result.HttpStatusCode}");
            return result.HttpStatusCode == (int) HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Inserts a new entity to the table
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entity">The entity to insert</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was inserted, <value>false</value> otherwise</returns>
        public async Task<bool> InsertEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                Trace.TraceVerbose($"Inserting entity '{entity.RowKey}' to the table");
                TableResult result =
                    await _cloudTable.ExecuteAsync(TableOperation.Insert(entity), cancellationToken);

                // a successful insert operation returns 201 (Created) or 204 (No Content)
                Trace.TraceVerbose(
                    $"Insert entity {entity.RowKey} completed with status code: {result.HttpStatusCode}");
                return result.HttpStatusCode == (int) HttpStatusCode.NoContent ||
                       result.HttpStatusCode == (int) HttpStatusCode.Created;
            }
            catch (StorageException exception)
            {
                // check if failed because of a conflict
                if (exception.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict)
                {
                    Trace.TraceInformation(
                        $"Insert entity {entity.RowKey} failed due to a conflict. StatusCode: {HttpStatusCode.Conflict}");
                    return false;
                }

                // other errors, rethrow
                Trace.TraceError($"Insert entity {entity.RowKey} failed. Exception: {exception}");
                throw;
            }
        }

        /// <summary>
        /// Merges the input entity with the stored entity with the same RowKey 
        /// by updating any non-null property in the input entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entity">The entity to merge</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was updated, <value>false</value> otherwise</returns>
        public async Task<bool> MergeEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
            where TEntity : TableEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            try
            {
                Trace.TraceVerbose($"Merging entity {entity.RowKey} to the table");
                TableResult result = await _cloudTable.ExecuteAsync(TableOperation.Merge(entity), cancellationToken);

                // a successful operation returns status code 204 (No Content). 
                Trace.TraceVerbose($"Done merging entity {entity.RowKey}. StatusCode: {result.HttpStatusCode}");
                return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
            }
            catch (StorageException exception)
            {
                // check if failed because of a conflict
                if (exception.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    Trace.TraceInformation($"Failed merging entity {entity.RowKey} due to conflicts. StatusCode: {HttpStatusCode.PreconditionFailed}");
                    return false;
                }

                // other errors, rethrow
                Trace.TraceError($"Failed merging entity {entity.RowKey}. Exception: {exception}");
                throw;
            }
        }

        /// <summary>
        /// Deletes an entity from the table by id
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entityId">The entityId of the entity to delete</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was deleted, <value>false</value> if the entity was not found</returns>
        public async Task<bool> DeleteEntityAsync<TEntity>(string entityId, CancellationToken cancellationToken)
            where TEntity : TableEntity
        {
            if (entityId == null)
            {
                throw new ArgumentNullException(nameof(entityId));
            }

            try
            {
                // create a delete entity operation
                TableOperation deleteEntityOperation =
                    TableOperation.Delete(new TableEntity(typeof(TEntity).Name, entityId) {ETag = "*"});

                Trace.TraceVerbose($"Deleting entity '{entityId}' from the table");
                TableResult result = await _cloudTable.ExecuteAsync(deleteEntityOperation, cancellationToken);

                // a successful delete operation returns status code 204 (No Content)
                Trace.TraceVerbose($"Deleting entity '{entityId}' completed with status code: {result.HttpStatusCode}");
                return result.HttpStatusCode == (int) HttpStatusCode.NoContent;
            }
            catch (StorageException exception)
            {
                // check if the entity didn't exist
                if (exception.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    Trace.TraceInformation($"Entity with {entityId} doesn't exist, skipping deletion");
                    return false;
                }

                Trace.TraceError($"Failed deleting entity {entityId}. Exception: {exception}");
                throw;
            }
        }

        private readonly CloudTable _cloudTable;
        private static readonly ITracer Trace = new Tracer(nameof(AzureCloudTable));
    }
}