// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights.TraceListener;
using Recommendations.Core;

namespace Recommendations.Common
{
    /// <summary>
    /// Provides a set of methods for tracing.
    /// </summary>
    public class Tracer : ITracer
    {
        /// <summary>
        /// Static constructor for initializing Application Insights trace listener
        /// </summary>
        static Tracer()
        {
            var instrumentationKey = ConfigurationManager.AppSettings["ApplicationInsightsInstrumentationKey"];
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                ApplicationInsightsListener = new ApplicationInsightsTraceListener(instrumentationKey);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Tracer"/> class.
        /// </summary>
        /// <param name="name">The name of the trace source</param>
        public Tracer(string name) 
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            _traceSource = new TraceSource(name, SourceLevels.All);
            if (ApplicationInsightsListener != null)
            {
                _traceSource.Listeners.Add(ApplicationInsightsListener);
            }

            // get the short version of the machine instance id
            _shortInstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")?.Substring(0, 6) ?? "local";
        }

        /// <summary>
        /// Writes a verbose message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceVerbose(string message, [CallerMemberName] string callerName = "")
        {
            Trace(TraceEventType.Verbose, message, callerName);
        }

        /// <summary>
        /// Writes an information message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceInformation(string message, [CallerMemberName] string callerName = "")
        {
            Trace(TraceEventType.Information, message, callerName);
        }

        /// <summary>
        /// Writes a warning message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceWarning(string message, [CallerMemberName] string callerName = "")
        {
            Trace(TraceEventType.Warning, message, callerName);
        }

        /// <summary>
        /// Writes an error message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceError(string message, [CallerMemberName] string callerName = "")
        {
            Trace(TraceEventType.Error, message, callerName);
        }

        private void Trace(TraceEventType type, string message, string callerName)
        {
            // try getting the model Id and role name from context
            string modelId = ContextManager.ModelId?.ToString() ?? "n/a";
            string roleName = ContextManager.RoleName ?? "n/a";

            // trace the formatted message
            _traceSource.TraceEvent(type, 0,
                $"{DateTime.UtcNow:O}\tInstanceId={_shortInstanceId}\tModelId={modelId}\t[{roleName}]\t[{callerName}]\t{message}");
        }

        private readonly string _shortInstanceId;
        private readonly TraceSource _traceSource;
        private static readonly TraceListener ApplicationInsightsListener;
    }
}
