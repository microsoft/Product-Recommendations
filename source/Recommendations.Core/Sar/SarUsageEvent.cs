// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.MachineLearning.Api;
using Microsoft.MachineLearning.Data;
using Newtonsoft.Json;

namespace Recommendations.Core.Sar
{
    /// <summary>
    /// A representation of a single usage event used in SAR training 
    /// </summary>
    internal class SarUsageEvent
    {
        /// <summary>
        /// The user id
        /// </summary>
        [ColumnName("user")]
        public uint UserId = 0;

        /// <summary>
        /// The item id
        /// </summary>
        [ColumnName("Item")]
        public uint ItemId = 0;

        /// <summary>
        /// The event timestamp
        /// </summary>
        [ColumnName("date"), JsonIgnore]
        public DvDateTime Timestamp = DvDateTime.NA;

        /// <summary>
        /// The event weight
        /// </summary>
        [ColumnName("weight")]
        public float Weight = 1;

        /// <summary>
        /// Used only for JSON serialization
        /// </summary>
        public DateTime? TimestampAsDateTime
        {
            get { return (DateTime?) Timestamp; }
            set { Timestamp = value; }
        }
    }
}