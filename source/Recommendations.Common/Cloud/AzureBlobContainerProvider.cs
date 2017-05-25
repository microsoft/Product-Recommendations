// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// An implementation of <see cref="IBlobContainerProvider"/> using an underlying Azure storage blob client
    /// </summary>
    public class AzureBlobContainerProvider : IBlobContainerProvider
    {
        /// <summary>
        /// Creates a new instance if the <see cref="AzureBlobContainerProvider"/> class.
        /// </summary>
        /// <param name="client">The underlying cloud blob client to use</param>
        public AzureBlobContainerProvider(CloudBlobClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        /// <summary>
        /// Gets a blob container reference
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="createIfNotExists">Indicates whether to create the container if it doesn't exists</param>
        /// <returns>An instance of <see cref="IBlobContainer"/> associated with the requested container</returns>
        public IBlobContainer GetBlobContainer(string containerName, bool createIfNotExists = false)
        {
            if (containerName == null)
            {
                throw new ArgumentNullException(nameof(containerName));
            }

            // get a cloud blob container from the client
            CloudBlobContainer container = _client.GetContainerReference(containerName);

            // create the container if requested
            if (createIfNotExists)
            {
                container.CreateIfNotExists();
            }

            // create and return a container 
            return new AzureBlobContainer(container);
        }

        private readonly CloudBlobClient _client;
    }
}