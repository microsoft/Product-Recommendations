// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Net;
using Microsoft.Azure.WebJobs;
using Recommendations.Common;

namespace Recommendations.WebJob
{
    class Program
    {
        static void Main()
        {
            ContextManager.RoleName = "WebJob";
            ServicePointManager.DefaultConnectionLimit = 1000;

            var config = new JobHostConfiguration
            {
                Queues =
                {
                    // Process only 1 message per instance of a web job
                    BatchSize = 1,

                    // Retry 2 times before giving up
                    MaxDequeueCount = 2
                }
            };
            
            // The following code ensures that the WebJob will be running continuously
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
