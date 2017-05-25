// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;

namespace Recommendations.Core
{
    /// <summary>
    /// a default implementation of the <see cref="ITracer"/> interface.
    /// </summary>
    internal class DefaultTracer : ITracer
    {
        /// <summary>
        /// Writes a verbose message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceVerbose(string message, string callerName = "")
        {
            Trace.WriteLine(message);
        }

        /// <summary>
        /// Writes an information message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceInformation(string message, string callerName = "")
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Writes a warning message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceWarning(string message, string callerName = "")
        {
            Trace.TraceWarning(message);
        }

        /// <summary>
        /// Writes an error message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        public void TraceError(string message, string callerName = "")
        {
            Trace.TraceError(message);
        }
    }
}
