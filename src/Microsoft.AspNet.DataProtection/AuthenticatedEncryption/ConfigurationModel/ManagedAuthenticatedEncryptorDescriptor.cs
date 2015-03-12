﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// A descriptor which can create an authenticated encryption system based upon the
    /// configuration provided by an <see cref="ManagedAuthenticatedEncryptionOptions"/> object.
    /// </summary>
    public sealed class ManagedAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
    {
        private readonly ILogger _log;

        public ManagedAuthenticatedEncryptorDescriptor([NotNull] ManagedAuthenticatedEncryptionOptions options, [NotNull] ISecret masterKey)
            : this(options, masterKey, services: null)
        {
        }

        public ManagedAuthenticatedEncryptorDescriptor([NotNull] ManagedAuthenticatedEncryptionOptions options, [NotNull] ISecret masterKey, IServiceProvider services)
        {
            Options = options;
            MasterKey = masterKey;
            _log = services.GetLogger<ManagedAuthenticatedEncryptorDescriptor>();
        }

        internal ISecret MasterKey { get; }

        internal ManagedAuthenticatedEncryptionOptions Options { get; }

        public IAuthenticatedEncryptor CreateEncryptorInstance()
        {
            return Options.CreateAuthenticatedEncryptorInstance(MasterKey, _log);
        }

        public XmlSerializedDescriptorInfo ExportToXml()
        {
            // <descriptor>
            //   <!-- managed implementations -->
            //   <encryption algorithm="..." keyLength="..." />
            //   <validation algorithm="..." />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", TypeToFriendlyName(Options.EncryptionAlgorithmType)),
                new XAttribute("keyLength", Options.EncryptionAlgorithmKeySize));

            var validationElement = new XElement("validation",
                new XAttribute("algorithm", TypeToFriendlyName(Options.ValidationAlgorithmType)));

            var rootElement = new XElement("descriptor",
                new XComment(" Algorithms provided by specified SymmetricAlgorithm and KeyedHashAlgorithm "),
                encryptionElement,
                validationElement,
                MasterKey.ToMasterKeyElement());

            return new XmlSerializedDescriptorInfo(rootElement, typeof(ManagedAuthenticatedEncryptorDescriptorDeserializer));
        }

        // Any changes to this method should also be be reflected
        // in ManagedAuthenticatedEncryptorDescriptorDeserializer.FriendlyNameToType.
        private static string TypeToFriendlyName(Type type)
        {
            if (type == typeof(Aes))
            {
                return nameof(Aes);
            }
            else if (type == typeof(HMACSHA1))
            {
                return nameof(HMACSHA1);
            }
            else if (type == typeof(HMACSHA256))
            {
                return nameof(HMACSHA256);
            }
            else if (type == typeof(HMACSHA384))
            {
                return nameof(HMACSHA384);
            }
            else if (type == typeof(HMACSHA512))
            {
                return nameof(HMACSHA512);
            }
            else
            {
                return type.AssemblyQualifiedName;
            }
        }
    }
}
