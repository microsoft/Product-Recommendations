// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http.Description;
using Microsoft.Win32.SafeHandles;
using Swashbuckle.Swagger;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Removed any <see cref="CancellationToken"/> and related parameters from controllers method when generating the swagger
    /// </summary>
    internal class SwaggerRemoveCancellationTokenParameterFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            // look for any cancellation tokens (or related) parameters 
            IEnumerable<Parameter> cancellationTokenParameters =
                apiDescription.ParameterDescriptions.Where(
                    pd =>
                        pd.ParameterDescriptor.ParameterType == typeof (CancellationToken) ||
                        pd.ParameterDescriptor.ParameterType == typeof (WaitHandle) ||
                        pd.ParameterDescriptor.ParameterType == typeof (SafeWaitHandle))
                    .Select(pd => operation.parameters?.FirstOrDefault(p => p.name == pd.Name))
                    .Where(parameter => parameter != null);

            // remove the found parameters from the operation
            foreach (Parameter cancellationTokenParameter in cancellationTokenParameters)
            {
                operation.parameters?.Remove(cancellationTokenParameter);
            }
        }
    }
}