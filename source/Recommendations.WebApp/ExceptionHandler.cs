// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Recommendations.Common;
using Recommendations.Core;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Handles all exception thrown in the controller
    /// </summary>
    internal class ExceptionHandler : DelegatingHandler
    {
        /// <summary>
        /// Hide exception message and stack trace from 500 errors.
        /// </summary>
        /// <param name="requestMessage">The HTTP request message</param>
        /// <param name="cancellationToken">The cancellation token for the operation</param>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage,
            CancellationToken cancellationToken)
        {
            ContextManager.RoleName = "WebApp";

            string requestString = $"'{requestMessage.Method} {requestMessage.RequestUri.PathAndQuery}'";
            Trace.TraceVerbose($"Handling request {requestString}");

            // send the request
            HttpResponseMessage responseMessage = await base.SendAsync(requestMessage, cancellationToken);

            Trace.TraceVerbose(responseMessage.IsSuccessStatusCode
                ? $"Request {requestString} completed successfully with status code {responseMessage.StatusCode}"
                : $"Request {requestString} failed with status code {responseMessage.StatusCode}: '{responseMessage.ReasonPhrase}'");

            //  hide internal error details from non-admins
            if (responseMessage.StatusCode == HttpStatusCode.InternalServerError
                && !_authorization.IsAuthorized(requestMessage.Headers))
            {
                // return a general error message
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
            
            return responseMessage;
        }

        private readonly ApiKeyAuthorizationFilterAttribute _authorization =
            new ApiKeyAuthorizationFilterAttribute(
                AuthorizationAppSettingsKeys.AdminPrimaryKey,
                AuthorizationAppSettingsKeys.AdminSecondaryKey);

        private static readonly ITracer Trace = new Tracer(nameof(ExceptionHandler));
    }
}