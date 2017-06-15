// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Recommendations.Common;
using Recommendations.Core;

namespace Recommendations.WebApp.Models
{
    /// <summary>
    /// Custom validation methods for <see cref="ModelParameters"/> class properties
    /// </summary>
    public partial class ModelParameters
    {
        /// <summary>
        /// A custom validation that validates that the input string represents an existing Azure blob container name
        /// </summary>
        public static ValidationResult ValidateBlobContainerName(string blobContainerName, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(blobContainerName))
            {
                return new ValidationResult($"The {context.DisplayName} field is required.");
            }
            
            try
            {
                // get a container reference
                CloudBlobContainer container = GetBlobContainerIfExists(blobContainerName);

                // check if the container exists
                if (container == null)
                {
                    return new ValidationResult(
                        $"The usage/catalog/evaluation Azure blob container specified in '{context.DisplayName}' does not exist in the service's default Azure storage account");
                }
            }
            catch (Exception exception)
            {
                // could not verify folder contents, continue anyways
                Trace.TraceWarning($"Unexpected exception '{exception}' - Internal API '{nameof(ValidateBlobContainerName)}'");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// A custom validation that validates that the catalog block blob exists under the base container, or that cold item placement is disabled
        /// </summary>
        public static ValidationResult ValidateCatalogBlobRelativePath(string catalogBlobRelativePath, ValidationContext context)
        {
            var modelParameters = (ModelParameters)context.ObjectInstance;

            try
            {
                // catalog is not mandatory if cold item placement is disabled
                if (!(modelParameters.EnableColdItemPlacement ?? false))
                {
                    return ValidationResult.Success;
                }

                if (string.IsNullOrWhiteSpace(catalogBlobRelativePath))
                {
                    return new ValidationResult($"{context.DisplayName} is required since enableColdItemPlacement value is true.");
                }

                // get the container
                CloudBlobContainer container = GetBlobContainerIfExists(modelParameters.BlobContainerName);
                if (container == null)
                {
                    // the container is invalid - skip this validation
                    return ValidationResult.Success;
                }

                // validate catalog exists
                var blockBlob = container.GetBlockBlobReference(catalogBlobRelativePath.Replace('\\', '/').Trim('/'));
                if (!blockBlob.Exists())
                {
                    return new ValidationResult($"{context.DisplayName} does not exist under the base container");
                }
            }
            catch (Exception e)
            {
                // could not verify blob exists, continue anyways
                Trace.TraceInformation($"Unexpected exception '{e}' - Internal API '{nameof(ValidateCatalogBlobRelativePath)}'");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// A custom validation that validates that the input (if provided) represents either a blob file or a non-empty Azure blob directory
        /// </summary>
        public static ValidationResult ValidateBlobExistsOrANonEmptyBlobDirectory(string blobOrDirectoryRelativePath, ValidationContext context)
        {
            var modelParameters = (ModelParameters)context.ObjectInstance;
            try
            {
                if (string.IsNullOrWhiteSpace(blobOrDirectoryRelativePath))
                {
                    return ValidationResult.Success;
                }

                // create the container
                CloudBlobContainer container = GetBlobContainerIfExists(modelParameters.BlobContainerName);
                if (container == null)
                {
                    // the container is invalid - skip this validation
                    return ValidationResult.Success;
                }

                blobOrDirectoryRelativePath = blobOrDirectoryRelativePath.Replace('\\', '/').Trim('/');

                // check if the input path represents a single blob
                if (container.GetBlobReference(blobOrDirectoryRelativePath).Exists())
                {
                    return ValidationResult.Success;
                }

                // create a reference to the blob directory
                CloudBlobDirectory blobDirectory = container.GetDirectoryReference(blobOrDirectoryRelativePath);

                // list the directory blobs to validate at least one block blob exists
                BlobResultSegment segment =
                    blobDirectory.ListBlobsSegmented(false, BlobListingDetails.None, 1, null, null, null);
                if (!segment.Results.Any())
                {
                    return new ValidationResult($"{context.DisplayName} does not contain any blobs");
                }
            }
            catch (Exception e)
            {
                // could not verify folder contents, continue anyways
                Trace.TraceWarning($"Unexpected exception '{e}' - Internal API '{nameof(ValidateBlobExistsOrANonEmptyBlobDirectory)}'");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// Gets a reference to a blob container in the service default storage account
        /// </summary>
        private static CloudBlobContainer GetBlobContainerIfExists(string containerName)
        {
            try
            {
                // get the storage connection string from configuration
                var connectionString = ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"];

                // parse the connection string
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString.ConnectionString);

                // create a blob client
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // create a container
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);

                // return the container if it exists and null otherwise
                return container.Exists() ? container : null;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.BadRequest)
                {
                    // the provided name is not a valid Azure blob container name
                    return null;
                }

                throw;
            }
        }

        private static readonly ITracer Trace = new Tracer(nameof(ModelParameters));
    }
}