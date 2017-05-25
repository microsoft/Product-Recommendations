// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// Representing a logical entity table
    /// </summary>
    public interface ITable
    {
        /// <summary>
        /// Lists all the table entities
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <param name="selectColumns">Optional list of column names for projection.</param>
        /// <returns>A list of all the table entities in the partition</returns>
        Task<IList<TEntity>> ListEntitiesAsync<TEntity>(CancellationToken cancellationToken, params string[] selectColumns)
            where TEntity : TableEntity, new();

        /// <summary>
        /// Gets a table entity by id
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entityId">The id of the entity</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <param name="selectColumns">List of column names for projection.</param>
        /// <returns>The retrieved entity of <value>null</value> if not found</returns>
        Task<TEntity> GetEntityAsync<TEntity>(string entityId, CancellationToken cancellationToken, params string[] selectColumns)
            where TEntity : TableEntity;

        /// <summary>
        /// Gets a table entity by id synchronously
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entityId">The id of the entity</param>
        /// <param name="selectColumns">List of column names for projection.</param>
        /// <returns>The retrieved entity of <value>null</value> if not found</returns>
        TEntity GetEntity<TEntity>(string entityId, params string[] selectColumns)
            where TEntity : TableEntity;

        /// <summary>
        /// Inserts or replaces a table entity
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entity">The entity to insert\update</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was inserted\updated, <value>false</value> otherwise</returns>
        Task<bool> InsertOrReplaceEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
            where TEntity : TableEntity;

        /// <summary>
        /// Inserts a new entity to the table
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entity">The entity to insert</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was inserted, <value>false</value> otherwise</returns>
        Task<bool> InsertEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
            where TEntity : TableEntity;

        /// <summary>
        /// Merges the input entity with the stored entity with the same RowKey 
        /// by updating any non-null property in the input entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entity">The entity to merge</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was updated, <value>false</value> otherwise</returns>
        Task<bool> MergeEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
            where TEntity : TableEntity;

        /// <summary>
        /// Deletes an entity from the table by id
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="entityId">The entityId of the entity to delete</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the entity was deleted, <value>false</value> if the entity was not found</returns>
        Task<bool> DeleteEntityAsync<TEntity>(string entityId, CancellationToken cancellationToken)
            where TEntity : TableEntity;
    }
}
