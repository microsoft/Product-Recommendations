// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Recommendations.Core
{
    /// <summary>
    /// An interface for a message tracer
    /// </summary>
    public interface ITracer
    {
        /// <summary>
        /// Writes a verbose message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        void TraceVerbose(string message, [CallerMemberName] string callerName = "");

        /// <summary>
        /// Writes an information message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        void TraceInformation(string message, [CallerMemberName] string callerName = "");

        /// <summary>
        /// Writes a warning message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        void TraceWarning(string message, [CallerMemberName] string callerName = "");

        /// <summary>
        /// Writes an error message to the trace listeners using the specified message.
        /// </summary>
        /// <param name="message">The informative message to write</param>
        /// <param name="callerName">The name of the calling method</param>
        void TraceError(string message, [CallerMemberName] string callerName = "");
    }
}