// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.DataProtection.XmlEncryption
{
    /// <summary>
    /// Wraps an <see cref="XElement"/> that contains a blob of encrypted XML
    /// and information about the class which can be used to decrypt it.
    /// </summary>
    public sealed class EncryptedXmlInfo
    {
        /// <summary>
        /// Creates an instance of an <see cref="EncryptedXmlInfo"/>.
        /// </summary>
        /// <param name="encryptedElement">A piece of encrypted XML.</param>
        /// <param name="decryptorType">The class whose <see cref="IXmlDecryptor.Decrypt(XElement)"/>
        /// method can be used to decrypt <paramref name="encryptedElement"/>.</param>
        public EncryptedXmlInfo([NotNull] XElement encryptedElement, [NotNull] Type decryptorType)
        {
            if (!typeof(IXmlDecryptor).IsAssignableFrom(decryptorType))
            {
                throw new ArgumentException(
                    Resources.FormatTypeExtensions_BadCast(decryptorType.FullName, typeof(IXmlDecryptor).FullName),
                    nameof(decryptorType));
            }

            EncryptedElement = encryptedElement;
            DecryptorType = decryptorType;
        }

        /// <summary>
        /// The class whose <see cref="IXmlDecryptor.Decrypt(XElement)"/> method can be used to
        /// decrypt the value stored in <see cref="EncryptedElement"/>.
        /// </summary>
        public Type DecryptorType { get; }

        /// <summary>
        /// A piece of encrypted XML.
        /// </summary>
        public XElement EncryptedElement { get; }
    }
}
