// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
namespace Recommendations.Core.Parsing
{
    public enum ParsingErrorReason
    {
        /// <summary>
        /// The line is in an invalid CSV format
        /// </summary>
        MalformedLine = 0,

        /// <summary>
        /// The line is missing some mandatory fields
        /// </summary>
        MissingFields = 1,

        /// <summary>
        /// The time stamp field is malformed
        /// </summary>
        BadTimestampFormat = 2,

        /// <summary>
        /// The event weight field is not numeric
        /// </summary>
        BadWeightFormat = 3,

        /// <summary>
        /// Some catalog item feature has a malformed format
        /// </summary>
        MalformedCatalogItemFeature = 4,

        /// <summary>
        /// The item id string is longer than the maximum allowed
        /// </summary>
        ItemIdTooLong = 5,

        /// <summary>
        /// The item id string contains invalid characters.
        /// </summary>
        IllegalCharactersInItemId = 6,

        /// <summary>
        /// The user id string is longer than the maximum allowed
        /// </summary>
        UserIdTooLong = 7,

        /// <summary>
        /// The user id string contains invalid characters.
        /// </summary>
        IllegalCharactersInUserId = 8,

        /// <summary>
        /// The item id doesn't appear in the catalog
        /// </summary>
        UnknownItemId = 9,

        /// <summary>
        /// The item id appears more than once in the catalog
        /// </summary>
        DuplicateItemId = 10


    }
}