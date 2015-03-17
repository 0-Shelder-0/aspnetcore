﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace Microsoft.AspNet.DataProtection.KeyManagement
{
    /// <summary>
    /// The basic implementation of <see cref="IKey"/>, where the <see cref="IAuthenticatedEncryptorDescriptor"/>
    /// has already been created.
    /// </summary>
    internal sealed class Key : KeyBase
    {
        public Key(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate, IAuthenticatedEncryptorDescriptor descriptor)
            : base(keyId, creationDate, activationDate, expirationDate, new Lazy<IAuthenticatedEncryptor>(descriptor.CreateEncryptorInstance))
        {
        }
    }
}
