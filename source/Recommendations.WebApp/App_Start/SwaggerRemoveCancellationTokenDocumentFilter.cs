// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Web.Http.Description;
using Microsoft.Win32.SafeHandles;
using Swashbuckle.Swagger;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Removed any <see cref="CancellationToken"/> and related parameters from controllers method when generating the swagger
    /// </summary>
    internal class SwaggerRemoveCancellationTokenDocumentFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            foreach (Type type in new[] {typeof (CancellationToken), typeof (WaitHandle), typeof (SafeWaitHandle)})
            {
                swaggerDoc.definitions?.Remove(type.Name);
            }
        }
    }
}