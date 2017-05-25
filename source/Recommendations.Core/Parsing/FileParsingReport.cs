// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Recommendations.Core.Parsing
{
    /// <summary>
    /// Represents a file parsing report
    /// </summary>
    public class FileParsingReport
    {
        /// <summary>
        /// Gets or set a value indicating whether the file parsing was completed successfully
        /// </summary>
        public bool IsCompletedSuccessfuly { get; set; }

        /// <summary>
        /// Gets or sets the total number of items parsed
        /// </summary>
        public int TotalLinesCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of items parsed successfully
        /// </summary>
        public int SuccessfulLinesCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether some parsing errors were found
        /// </summary>
        public bool HasErrors => Errors?.Count > 0;

        /// <summary>
        /// Gets the list of found parsing errors
        /// </summary>
        public List<ParsingError> Errors { get; }

        /// <summary>
        /// Gets or sets a value indicating whether some parsing warnings were found
        /// </summary>
        public bool HasWarnings => Warnings?.Count > 0;

        /// <summary>
        /// Gets the list of found parsing warnings
        /// </summary>
        public List<ParsingError> Warnings { get; }

        /// <summary>
        /// Gets or sets the number of lines with items that were ignored because of an unknown id
        /// </summary>
        public long ItemsWithUnknownIdCount { get; set; }

        /// <summary>
        /// Gets a list of the unknown item ids found during parsing
        /// </summary>
        public HashSet<string> UnknownItemIds { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="FileParsingReport"/> class.
        /// </summary>
        public FileParsingReport()
        {
            Errors = new List<ParsingError>();
            Warnings = new List<ParsingError>();
            UnknownItemIds = new HashSet<string>();
        }
    }
}