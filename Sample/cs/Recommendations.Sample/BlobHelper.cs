// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Recommendations.Sample
{
    /// <summary>
    /// Helper for Blob Storage Access
    /// </summary>
    public class BlobHelper
    {

        /// <summary>
        /// The following constructor takes storage account and container name as input
        /// With this constructor, we can use any azure storage account and container name for the blob operations
        /// </summary>
        /// <param name="storageAccount">The storage account to work on the blob operations</param>
        /// <param name="containerName">The container name for the blobs. It is created if it does not exist.</param>
        public BlobHelper(CloudStorageAccount storageAccount, string containerName)
        {
            _account = storageAccount;
            _blobClient = _account.CreateCloudBlobClient();
            LinearRetry linearRetry = new LinearRetry(TimeSpan.FromMilliseconds(500), 3);
            _blobClient.DefaultRequestOptions.RetryPolicy = linearRetry;

            if (!string.IsNullOrEmpty(containerName))
            {
                _container = _blobClient.GetContainerReference(containerName);
                _container.CreateIfNotExists();
            }
        }

        /// <summary>
        /// This method is used mainly by unit test
        /// The source code uses it is for easier to be tested through unittest
        /// It is used to get the content of a cloud append blob
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        public string GetContent(CloudAppendBlob blob)
        {
            return blob.DownloadText();
        }

        /// <summary>
        /// Generate a shared access signature for a policy
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="policy"></param>
        /// <param name="signature"></param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        public bool GenerateSharedAccessSignature(string containerName, SharedAccessBlobPolicy policy, out string signature)
        {
            signature = null;

            try
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
                signature = container.GetSharedAccessSignature(policy);
                return true;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Generate a shared access signature for a saved container policy
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="policyName"></param>
        /// <param name="signature"></param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        public bool GenerateSharedAccessSignature(string containerName, string policyName, out string signature)
        {
            signature = null;

            try
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
                signature = container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), policyName);
                return true;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Enumerate the blobs in a container
        /// </summary>
        /// <param name="containerName">container name</param>
        /// <param name="blobList">out - the blobs, you should check the type and
        /// cast accordingly to CloudBlockBlob or CloudPageBlob</param>
        /// <param name="prefix"> blobs to list name prefix, no prefix if null</param>
        /// <returns>Return true on success, false if not found, throw exception on error</returns>
        public bool ListBlobs(string containerName, out IEnumerable<IListBlobItem> blobList, string prefix = null)
        {
            blobList = new List<IListBlobItem>();

            try
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);

                //List blobs in this container using a flat listing.
                blobList = container.ListBlobs(prefix, true);
                // we call count to force the method to execute the listblobs
                // since it is done lazly and then we will get expcetion in 
                // unexpected location
                blobList.Count();

                return true;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }

                throw;
            }
        }


        /// <summary>
        /// The following method lists the blobs under the default container (with some prefix if the prefix is set)
        /// </summary>
        /// <param name="blobList">The blob list</param>
        /// <param name="prefix">The prefix of the blob name</param>
        /// <returns>success or not</returns>
        public bool ListBlobs(out IEnumerable<IListBlobItem> blobList, string prefix = null)
        {
            if (_container == null)
            {
                blobList = null;
                return false;
            }

            return ListBlobs(_container.Name, out blobList, prefix);
        }


        /// <summary>
        /// Queries for a specific blob-name inside a blob container
        /// </summary>
        /// <returns>True if found, false if not fount and throws on exception</returns>
        public bool IsExist(string containerName, string blobName)
        {
            IEnumerable<IListBlobItem> blobList;
            if (!ListBlobs(containerName, out blobList))
            {
                return false;
            }
            return blobList.Any(cloudBlob => ((ICloudBlob)cloudBlob).Name.Equals(blobName));
        }


        /// <summary>
        /// Get a Stream that enables to read from a blob
        /// </summary>
        /// <param name="containerName">the container name</param>
        /// <param name="blobName">the blob name</param>
        /// <returns>the BlobStream that enables to read data from the blob</returns>
        public Stream GetBlobReader(string containerName, string blobName)
        {
            if (!IsExist(containerName, blobName)) return null;
            CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
            ICloudBlob blobReferenceFromServer = container.GetBlobReferenceFromServer(blobName);
            BlobRequestOptions opt = new BlobRequestOptions { MaximumExecutionTime = GetDataTimeout };
            return blobReferenceFromServer.OpenRead(null, opt);
        }


        /// <summary>
        /// Put (create or update) a block blob
        /// </summary>
        /// <param name="containerName">container name</param>
        /// <param name="blobName">blob name</param>
        /// <param name="content">string content</param>
        /// <returns>Return true on success, false if unable to create, throw exception on error</returns>
        public bool PutBlockBlob(string containerName, string blobName, string content)
        {
            try
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
                MemoryStream ms = new MemoryStream(Convert(content));
                blob.UploadFromStream(ms);
                return true;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Get (retrieve) a blob and return its content
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="content"></param>
        /// <returns>true if the blob was found, false if the blob wasn't found, throws exception in all other cases</returns>
        public bool GetBlob(string containerName, string blobName, out string content)
        {
            content = null;

            try
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
                ICloudBlob blob = container.GetBlobReferenceFromServer(blobName);
                MemoryStream ms = new MemoryStream();
                blob.DownloadToStream(ms);
                byte[] array = ms.ToArray();
                content = Convert(array);
                return true;
            }
            catch (StorageException ex)
            {
                // if blob not found
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Convert string into byte[]
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] Convert(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        /// <summary>
        /// Convert byte[] into string
        /// </summary>
        /// <returns></returns>
        public static string Convert(byte[] buffer)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetString(buffer);
        }


        public static string GenerateBlobSasToken(string connectionString, string inputContainerName, string inputBlobName)
        {
            var sourceStorageAccount = CloudStorageAccount.Parse(connectionString);
            // generate the sas token with write permission in customer's storage and pass it to the command worker role
            var sourceBlobClient = sourceStorageAccount.CreateCloudBlobClient();

            var container = sourceBlobClient.GetContainerReference(inputContainerName);
            var blob = container.GetAppendBlobReference(inputBlobName);

            //Set the expiry time and permissions for the blob.
            //In this case the start time is specified as a few minutes in the past, to mitigate clock skew.
            //The shared access signature will be valid immediately.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(72);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write;

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

            return sasBlobToken;
        }


        /// <summary>
        /// Get Blob data connection timeout
        /// </summary>
        private static readonly TimeSpan GetDataTimeout = new TimeSpan(1, 0, 0); // 1H

        /// <summary>
        /// Container reference for operations on same container
        /// </summary>
        private readonly CloudBlobContainer _container;

        /// <summary>
        /// Hold reference to the storage account
        /// </summary>
        private readonly CloudStorageAccount _account;

        /// <summary>
        /// Holds the storage client
        /// </summary>
        private readonly CloudBlobClient _blobClient;
    }
}
