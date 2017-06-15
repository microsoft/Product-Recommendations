// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// Representing a logical blob files container
    /// </summary>
    public interface IBlobContainer
    {
        /// <summary>
        /// Uploads a content stream to a blob in the container. The blob will be created or overwritten if exists.
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="content">The content of the blob</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        Task UploadBlobAsync(string blobName, Stream content, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads a blob content into a stream
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="targetStream">The stream to download the blob to</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        Task DownloadBlobAsync(string blobName, Stream targetStream, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads a blob content into a local file
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="targetFilePath">The file path to download the blob to</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        Task DownloadBlobAsync(string blobName, string targetFilePath, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a blob from the container
        /// </summary>
        /// <param name="blobName">The name of the blob to remove</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the blob was deleted or <value>false</value> if the blob wasn't found</returns>
        Task<bool> DeleteBlobIfExistsAsync(string blobName, CancellationToken cancellationToken);

        /// <summary>
        /// Check if a blob exists.
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> is the blob exists, <value>false</value> otherwise</returns>
        Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken);

        /// <summary>
        /// Lists all the blob names from directly under a directory, excluding subdirectories
        /// </summary>
        /// <param name="directoryName">The name of the directory to list</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns>A list for blob names found in the directory</returns>
        Task<IList<string>> ListBlobsAsync(string directoryName, CancellationToken cancellationToken);
    }
}
