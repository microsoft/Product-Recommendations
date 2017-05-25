// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.MachineLearning.Api;

namespace Recommendations.Core.Sar
{
    internal class SarScoreResult
    {
        [ColumnName("user")]
        public uint User;

        public uint Recommended;

        public float Score;
    }
}