// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Swashbuckle.Swagger;

namespace Recommendations.WebApp
{
    /// <summary>
    /// An implementation of <see cref="ISwaggerProvider"/> that caches the generated document 
    /// </summary>
    internal class CachingSwaggerProvider : ISwaggerProvider
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CachingSwaggerProvider"/> class.
        /// </summary>
        /// <param name="defaultProvider">The default swagger provider</param>
        public CachingSwaggerProvider(ISwaggerProvider defaultProvider)
        {
            _defaultProvider = defaultProvider;
        }

        /// <summary>
        /// Return the cached swagger document or generated it if doesn't exists. 
        /// </summary>
        public SwaggerDocument GetSwagger(string rootUrl, string apiVersion)
        {
            if (_cachedSwaggerDocument == null)
            {
                _cachedSwaggerDocument = _defaultProvider.GetSwagger(rootUrl, apiVersion);
            }

            return _cachedSwaggerDocument;
        }

        private SwaggerDocument _cachedSwaggerDocument;
        private readonly ISwaggerProvider _defaultProvider;
    }
}