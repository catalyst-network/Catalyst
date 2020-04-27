#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Cryptography.Proto;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Keystore;
using MultiFormats;
using NSubstitute;
using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using ProtoBuf;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.TestUtils
{
    public static class TestKeyRegistry
    {
        public static readonly string TestPrivateKey = "ftqm5kpzpo7bvl6e53q5j6mmrjwupbbiuszpsopxvjodkkqqiusa";
        public static readonly string TestPublicKey;

        static TestKeyRegistry()
        {
            var cryptoContext = new FfiWrapper();
            var fakePrivateKey = cryptoContext.GetPrivateKeyFromBytes(TestPrivateKey.FromBase32());
            TestPublicKey = fakePrivateKey.GetPublicKey().Bytes.KeyToString();
        }

        public static IKeyRegistry MockKeyRegistry()
        {
            var keyRegistry = Substitute.For<IKeyRegistry>();
            var cryptoContext = new FfiWrapper();
            var fakePrivateKey = cryptoContext.GetPrivateKeyFromBytes(TestPrivateKey.FromBase32());
            keyRegistry.GetItemFromRegistry(KeyRegistryTypes.DefaultKey).Returns(fakePrivateKey);
            keyRegistry.Contains(Arg.Any<byte[]>()).Returns(true);
            return keyRegistry;
        }

        public static IStore<string, Core.Lib.Cryptography.EncryptedKey> MockKeyFileStore()
        {
            var privateKeyBytes = TestPrivateKey.FromBase32();
            var privateKey = new Ed25519PrivateKeyParameters(privateKeyBytes, 0);
            var publicKey = privateKey.GeneratePublicKey();
            var keyId = CreateKeyId(publicKey);

            using var sw = new StringWriter();
            var pkcs8 = new Pkcs8Generator(privateKey, Pkcs8Generator.PbeSha1_3DES);
            pkcs8.Password = "test".ToCharArray();
            var pw = new PemWriter(sw);
            pw.WriteObject(pkcs8);
            pw.Writer.Flush();
            var pem = sw.ToString();

            var encryptedKey = new Core.Lib.Cryptography.EncryptedKey
            {
                Id = keyId.ToBase58(),
                Name = "self",
                Pem = pem
            };

            var keyFileStore = Substitute.For<IStore<string, Core.Lib.Cryptography.EncryptedKey>>();
            keyFileStore.TryGetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(encryptedKey));
            return keyFileStore;
        }

        public static MultiHash CreateKeyId(AsymmetricKeyParameter key)
        {
            var spki = SubjectPublicKeyInfoFactory
               .CreateSubjectPublicKeyInfo(key)
               .GetDerEncoded();

            // Add protobuf cruft.
            var publicKey = new Core.Lib.Cryptography.Proto.PublicKey
            {
                Data = spki
            };
            switch (key)
            {
                case RsaKeyParameters _:
                    publicKey.Type = KeyType.Rsa;
                    break;
                case ECPublicKeyParameters _:
                    publicKey.Type = KeyType.Secp256K1;
                    break;
                case Ed25519PublicKeyParameters _:
                    publicKey.Type = KeyType.Ed25519;
                    break;
                default:
                    throw new NotSupportedException($"The key type {key.GetType().Name} is not supported.");
            }

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, publicKey);

                // If the length of the serialized bytes <= 42, then we compute the "identity" multihash of 
                // the serialized bytes. The idea here is that if the serialized byte array 
                // is short enough, we can fit it in a multihash verbatim without having to 
                // condense it using a hash function.
                var alg = ms.Length <= 48 ? "identity" : "sha2-256";

                ms.Position = 0;

                return MultiHash.ComputeHash(ms, alg);
            }
        }
    }
}
