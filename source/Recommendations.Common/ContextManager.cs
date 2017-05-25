// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.Remoting.Messaging;

namespace Recommendations.Common
{
    /// <summary>
    /// A logical context that stores call information across threads using <see cref="CallContext"/>.
    /// </summary>
    public static class ContextManager
    {
        /// <summary>
        /// Gets or sets the model id in context
        /// </summary>
        public static object ModelId
        {
            get
            {
                return CallContext.LogicalGetData(nameof(ModelId)); 
            }

            set
            {
                CallContext.LogicalSetData(nameof(ModelId), value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the role in context
        /// </summary>
        public static string RoleName
        {
            get
            {
                return CallContext.LogicalGetData(nameof(RoleName))?.ToString();
            }

            set
            {
                CallContext.LogicalSetData(nameof(RoleName), value);
            }
        }
    }
}