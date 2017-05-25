// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Core.Parsing
{
    public class ParsingError
    {
        /// <summary>
        /// Gets the name of the file containing the error line
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the error line number
        /// </summary>
        public long LineNumber { get; }

        /// <summary>
        /// Gets the error reason
        /// </summary>
        public ParsingErrorReason ErrorReason { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ParsingError"/> class.
        /// </summary>
        /// <param name="fileName">The name of the file containing the error line</param>
        /// <param name="line">The error line number</param>
        /// <param name="reason">The error reason</param>
        public ParsingError(string fileName, long line, ParsingErrorReason reason)
        {
            FileName = fileName;
            LineNumber = line;
            ErrorReason = reason;
        }
    }
}