﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNet.Cryptography;

namespace Microsoft.AspNet.DataProtection.Managed
{
    internal static class SymmetricAlgorithmExtensions
    {
        public static int GetBlockSizeInBytes(this SymmetricAlgorithm symmetricAlgorithm)
        {
            var blockSizeInBits = symmetricAlgorithm.BlockSize;
            CryptoUtil.Assert(blockSizeInBits >= 0 && blockSizeInBits % 8 == 0, "blockSizeInBits >= 0 && blockSizeInBits % 8 == 0");
            return blockSizeInBits / 8;
        }
    }
}
