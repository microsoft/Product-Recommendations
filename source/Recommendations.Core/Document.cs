// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Recommendations.Core
{
    /// <summary>
    /// Represent a single document storable and retrievable by <see cref="IDocumentStore"/> implementations
    /// </summary>
    public sealed class Document
    {
        /// <summary>
        /// Gets the id of the document
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets the content of the document
        /// </summary>
        public string Content { get; set; }
    }
}