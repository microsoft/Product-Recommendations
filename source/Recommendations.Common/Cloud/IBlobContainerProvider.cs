// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Recommendations.Common.Cloud
{
    /// <summary>
    /// Representing a <see cref="IBlobContainer"/> provider
    /// </summary>
    public interface IBlobContainerProvider
    {
        /// <summary>
        /// Gets a blob container reference
        /// </summary>
        /// <param name="containerName">The name of the container</param>
        /// <param name="createIfNotExists">Indicates whether to create the container if it doesn't exists</param>
        /// <returns>An instance of <see cref="IBlobContainer"/> associated with the requested container</returns>
        IBlobContainer GetBlobContainer(string containerName, bool createIfNotExists = false);
    }
}