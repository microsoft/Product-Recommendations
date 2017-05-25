// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Recommendations.WebApp
{
    /// <summary>
    /// Filter to check authorization key
    /// </summary>
    internal class ApiKeyAuthorizationFilterAttribute : FilterAttribute, IAuthorizationFilter
    {
        /// <summary>
        /// The name of the application key HTTP header
        /// </summary>
        public const string ApiKeyHeaderName = "x-api-key";

        /// <summary>
        /// Initialize filter with app settings to authorization keys
        /// </summary>
        /// <param name="authorizationKeysAppSettingsKeys">App setting keys of authorization keys</param>
        public ApiKeyAuthorizationFilterAttribute(params string[] authorizationKeysAppSettingsKeys)
        {
            _authorizationKeysAppSettingsKeys = authorizationKeysAppSettingsKeys ?? new string[0];
        }

        /// <summary>
        /// Validates if the provided key in the header is authorized
        /// </summary>
        /// <param name="actionContext">The HTTP context</param>
        /// <param name="cancellationToken">The cancellation token for the operation</param>
        /// <param name="continuation">The continuation if the key is authorized</param>
        /// <returns>The HTTP response message</returns>
        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext,
            CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            if (!IsAuthorized(actionContext.Request.Headers))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            // authorized
            return continuation();
        }

        /// <summary>
        /// Check whether the request headers contain an authorized api key
        /// </summary>
        /// <param name="requestHeaders">The request headers</param>
        /// <returns><value>true</value> if the request contains a valid api key value, or <value>false</value> otherwise</returns>
        internal bool IsAuthorized(HttpRequestHeaders requestHeaders)
        {
            // get the api key value (ignore duplicate headers)
            IEnumerable<string> apiKeyHeaderValues = null;
            requestHeaders?.TryGetValues(ApiKeyHeaderName, out apiKeyHeaderValues);
            string apiKey = apiKeyHeaderValues?.FirstOrDefault();
            if (apiKey == null)
            {
                return false;
            }

            // lookup actual authorized keys from app settings
            List<string> authorizationKeys = _authorizationKeysAppSettingsKeys.Select(
                appSettingsKey => ConfigurationManager.AppSettings[appSettingsKey]).ToList();

            // check if the provided key is in the list of authorized keys
            return authorizationKeys.Contains(apiKey);
        }

        private readonly string[] _authorizationKeysAppSettingsKeys;
    }
}