// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.MachineLearning;
using Microsoft.MachineLearning.Data;

namespace Recommendations.Core.Sar
{
    internal static class LoggerExtensionMethods
    {
        /// <summary>
        /// Trace a TLC environment channel message to the trace source
        /// </summary>
        public static void TraceChannelMessage(this ITracer tracer, IMessageSource source, ChannelMessage channelMessage)
        {
            switch (channelMessage.Kind)
            {
                case ChannelMessageKind.Trace:
                    tracer.TraceVerbose($"{source?.FullName}:{channelMessage.Message}");
                    break;
                case ChannelMessageKind.Info:
                    tracer.TraceInformation($"{source?.FullName}:{channelMessage.Message}");
                    break;
                case ChannelMessageKind.Warning:
                    tracer.TraceWarning($"{source?.FullName}:{channelMessage.Message}");
                    break;
                case ChannelMessageKind.Error:
                    tracer.TraceError($"{source?.FullName}:{channelMessage.Message}");
                    break;
            }
        }
    }
}
