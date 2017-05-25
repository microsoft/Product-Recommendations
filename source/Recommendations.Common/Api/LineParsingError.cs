// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
               
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Recommendations.Core.Parsing;

namespace Recommendations.Common.Api
{
    /// <summary>
    /// A line parsing error information
    /// </summary>
    public class LineParsingError
    {
        /// <summary>
        /// Gets the error count
        /// </summary>
        [JsonProperty("count")]
        public int Count { get; set; }

        /// <summary>
        /// Gets the parsing error reason
        /// </summary>
        [JsonProperty("error"), JsonConverter(typeof(StringEnumConverter))]
        public ParsingErrorReason Error { get; set; }

        /// <summary>
        /// Gets the parsing error sample
        /// </summary>
        [JsonProperty("sample")]
        public ParsingErrorSample Sample { get; set; }
    }
}