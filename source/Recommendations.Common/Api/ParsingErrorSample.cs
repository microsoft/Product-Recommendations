// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace Recommendations.Common.Api
{
    /// <summary>
    /// A line parsing error sample
    /// </summary>
    public class ParsingErrorSample
    {
        /// <summary>
        /// The name and relative path of the file containing the error line
        /// </summary>
        [JsonProperty("file")]
        public string FileRelativePath { get; set; }

        /// <summary>
        /// The error line number
        /// </summary>
        [JsonProperty("line")]
        public long LineNumber { get; set; }
    }
}