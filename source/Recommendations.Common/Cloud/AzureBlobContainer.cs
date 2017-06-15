// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Recommendations.Core;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// An implementation of <see cref="IBlobContainer"/> using an underlying Azure storage blob container
    /// </summary>
    public class AzureBlobContainer : IBlobContainer
    {
        /// <summary>
        /// Creates a new instance if the <see cref="AzureBlobContainer"/> class.
        /// </summary>
        /// <param name="container">The underlying cloud blob container to use</param>
        public AzureBlobContainer(CloudBlobContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            _container = container;
        }

        /// <summary>
        /// Uploads a content stream to a blob in the container. The blob will be created or overwritten if exists.
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="content">The content of the blob</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        public Task UploadBlobAsync(string blobName, Stream content, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            
            Trace.TraceVerbose($"Uploading stream to blob with name '{blobName}'");

            // get a reference to the blob
            CloudBlockBlob blob = _container.GetBlockBlobReference(blobName);

            // upload the blob content
            return blob.UploadFromStreamAsync(content, cancellationToken);
        }

        /// <summary>
        /// Downloads a blob content into a stream
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="targetStream">The stream to download the blob to</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        public Task DownloadBlobAsync(string blobName, Stream targetStream, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            if (targetStream == null)
            {
                throw new ArgumentNullException(nameof(targetStream));
            }
            
            Trace.TraceVerbose($"Downloading blob '{blobName}' content to a stream");

            // get a reference to the blob
            CloudBlockBlob blob = _container.GetBlockBlobReference(blobName);

            // download the blob content
            return blob.DownloadToStreamAsync(targetStream, cancellationToken);
        }

        /// <summary>
        /// Downloads a blob content into a local file
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="targetFilePath">The file path to download the blob to</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        public Task DownloadBlobAsync(string blobName, string targetFilePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            if (string.IsNullOrWhiteSpace(targetFilePath))
            {
                throw new ArgumentNullException(nameof(targetFilePath));
            }

            Trace.TraceVerbose($"Downloading blob '{blobName}' content to a file");

            // get a reference to the blob
            CloudBlockBlob blob = _container.GetBlockBlobReference(blobName);

            // download the blob content
            return blob.DownloadToFileAsync(targetFilePath, FileMode.Create, cancellationToken);
        }

        /// <summary>
        /// Deletes a blob from the container
        /// </summary>
        /// <param name="blobName">The name of the blob to remove</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> if the blob was deleted or <value>false</value> if the blob wasn't found</returns>
        public Task<bool> DeleteBlobIfExistsAsync(string blobName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            Trace.TraceVerbose($"Deleting blob '{blobName}'");

            // get a reference to the blob
            CloudBlockBlob blob = _container.GetBlockBlobReference(blobName);

            // delete the blob
            return blob.DeleteIfExistsAsync(cancellationToken);
        }

        /// <summary>
        /// Check if a blob exists.
        /// </summary>
        /// <param name="blobName">The name of the blob</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns><value>true</value> is the blob exists, <value>false</value> otherwise</returns>
        public Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentNullException(nameof(blobName));
            }

            Trace.TraceVerbose($"Checking if blob '{blobName}' exists");

            // get a reference to the blob
            CloudBlockBlob blob = _container.GetBlockBlobReference(blobName);

            // check if blob exists
            return blob.ExistsAsync(cancellationToken);
        }

        /// <summary>
        /// Lists all the blob names from directly under a directory, excluding subdirectories
        /// </summary>
        /// <param name="directoryName">The name of the directory to list</param>
        /// <param name="cancellationToken">The cancellation token assigned for the operation</param>
        /// <returns>A list for blob names found in the directory</returns>
        public async Task<IList<string>> ListBlobsAsync(string directoryName, CancellationToken cancellationToken)
        {
            // get a reference to the directory
            CloudBlobDirectory blobDirectory = _container.GetDirectoryReference(directoryName ?? string.Empty);

            var blobsList = new List<string>();
            BlobContinuationToken token = null;

            Trace.TraceVerbose($"Listing all blobs under the '{directoryName}' directory");

            do
            {
                // get next blobs segment
                BlobResultSegment segment = await blobDirectory.ListBlobsSegmentedAsync(token, cancellationToken);

                // set the next token
                token = segment.ContinuationToken;

                // add segment results to the list 
                blobsList.AddRange(segment.Results.OfType<CloudBlob>().Select(blob => blob.Name));
            } while (token != null);

            Trace.TraceVerbose($"Found {blobsList.Count} blobs under the '{directoryName}' directory");
            return blobsList;
        }

        private readonly CloudBlobContainer _container;
        private static readonly ITracer Trace = new Tracer(nameof(AzureBlobContainer));
    }
}