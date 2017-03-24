// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET46 // [[ISSUE60]] Remove this #ifdef when Core CLR gets support for EncryptedXml

using System;
using System.Xml;
using System.Security.Cryptography.Xml;

namespace Microsoft.AspNetCore.DataProtection.XmlEncryption
{
    /// <summary>
    /// Internal implementation details of <see cref="CertificateXmlEncryptor"/> for unit testing.
    /// </summary>
    internal interface IInternalCertificateXmlEncryptor
    {
        EncryptedData PerformEncryption(EncryptedXml encryptedXml, XmlElement elementToEncrypt);
    }
}
#elif NETSTANDARD1_3
#else
#error target frameworks need to be updated.
#endif
