// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Web;
using Recommendations.Common;
using Recommendations.Common.Cloud;

namespace Recommendations.WebApp
{
    /// <summary>
    /// A static helper class for holding reference to common instances
    /// </summary>
    internal static class WebAppContext
    {
        /// <summary>
        /// Gets or sets a <see cref="ModelsProvider"/> instance
        /// </summary>
        public static ModelsProvider ModelsProvider
        {
            get
            {
                return HttpContext.Current.Application[nameof(ModelsProvider)] as ModelsProvider; 
            }

            set
            {
                HttpContext.Current.Application[nameof(ModelsProvider)] = value;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="ModelsRegistry"/> instance
        /// </summary>
        public static ModelsRegistry ModelsRegistry
        {
            get
            {
                return HttpContext.Current.Application[nameof(ModelsRegistry)] as ModelsRegistry;
            }

            set
            {
                HttpContext.Current.Application[nameof(ModelsRegistry)] = value;
            }
        }

        /// <summary>
        /// Gets or sets a train model queue client
        /// </summary>
        public static IModelQueue TrainModelQueue
        {
            get
            {
                return HttpContext.Current.Application[nameof(TrainModelQueue)] as IModelQueue;
            }

            set
            {
                HttpContext.Current.Application[nameof(TrainModelQueue)] = value;
            }
        }

        /// <summary>
        /// Gets or sets a delete model queue client
        /// </summary>
        public static IModelQueue DeleteModelQueue
        {
            get
            {
                return HttpContext.Current.Application[nameof(DeleteModelQueue)] as IModelQueue;
            }

            set
            {
                HttpContext.Current.Application[nameof(DeleteModelQueue)] = value;
            }
        }
    }
}