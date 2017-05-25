// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Recommendations.Common.Api
{
    /// <summary>
    /// A file parsing report information
    /// </summary>
    public class ParsingReport
    {

        /// <summary>
        /// The total parsing duration
        /// </summary>
        [JsonProperty("duration")]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// A list of line parsing errors
        /// </summary>
        [JsonProperty("errors")]
        public List<LineParsingError> Errors { get; set; }

        /// <summary>
        /// The total number of lines parsed successfully
        /// </summary>
        [JsonProperty("successfulLinesCount")]
        public int SuccessfulLinesCount { get; set; }

        /// <summary>
        /// The total number of lines parsed
        /// </summary>
        [JsonProperty("totalLinesCount")]
        public int TotalLinesCount { get; set; }
    }
}