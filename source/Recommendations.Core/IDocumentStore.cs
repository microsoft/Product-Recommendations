// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Recommendations.Core
{
    /// <summary>
    /// An interface for storing and retrieving <see cref="Document"/> instances
    /// </summary>
    public interface IDocumentStore
    {
        /// <summary>
        /// Creates the document store is not already exists. 
        /// </summary>
        /// <returns><value>true</value> if the document store was created; Otherwise, <value>false</value></returns>
        bool CreateIfNotExists();

        /// <summary>
        /// Gets a document from the store
        /// </summary>
        /// <param name="key">The document partition key</param>
        /// <param name="id">The id of the document</param>
        /// <returns>All the documents associated with the key</returns>
        Document GetDocument(string key, string id);

        /// <summary>
        /// Adds documents to the storage
        /// </summary>
        /// <param name="key">The key to add the documents under</param>
        /// <param name="documents">The documents to insert</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns>The number of successfully inserted items</returns>
        Task<int> AddDocumentsAsync(string key, IEnumerable<Document> documents, CancellationToken cancellationToken);
        
        /// <summary>
        /// Deletes the document store is exists. 
        /// </summary>
        /// <returns><value>true</value> if the document store was deleted; Otherwise, <value>false</value></returns>
        Task<bool> DeleteIfExistsAsync();
    }
}
