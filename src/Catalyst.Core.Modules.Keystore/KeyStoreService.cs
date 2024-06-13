#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.Cryptography.Proto;
using Catalyst.Core.Lib.FileSystem;
using Common.Logging;
using MultiFormats;
using Org.BouncyCastle.Asn1.EdEC;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using ProtoBuf;

namespace Catalyst.Core.Modules.Keystore
{
    /// <summary>
    ///     A secure key chain.
    /// </summary>
    public class KeyStoreService : IKeyStoreService
    {
        private readonly string _folder;
        private static readonly ILog Log = LogManager.GetLogger(typeof(KeyStoreService));

        private char[] _dek;
        private FileStore<string, EncryptedKey> _store;

        /// <summary>
        ///     Create a new instance of the <see cref="KeyStoreService" /> class.
        /// </summary>
        /// <param name="dfsOptions"></param>
        public KeyStoreService(DfsOptions dfsOptions)
        {
            _folder = dfsOptions.Repository.Folder;
            Options = dfsOptions.KeyChain;
        }

        private FileStore<string, EncryptedKey> Store
        {
            get
            {
                if (_store != null)
                {
                    return _store;
                }

                var folder = Path.Combine(_folder, "keys");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                _store = new FileStore<string, EncryptedKey>
                {
                    Folder = folder,
                    NameToKey = name => Encoding.UTF8.GetBytes(name).ToBase32(),
                    KeyToName = key => Encoding.UTF8.GetString(Base32.Decode(key))
                };

                return _store;
            }
        }

        /// <summary>
        ///     The configuration options.
        /// </summary>
        public KeyChainOptions Options { get; set; }

        /// <summary>
        ///     Encrypt data as CMS protected data.
        /// </summary>
        /// <param name="keyName">
        ///     The key name to protect the <paramref name="plainText" /> with.
        /// </param>
        /// <param name="plainText">
        ///     The data to protect.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     the cipher text of the <paramref name="plainText" />.
        /// </returns>
        /// <remarks>
        ///     Cryptographic Message Syntax (CMS), aka PKCS #7 and
        ///     <see href="https://tools.ietf.org/html/rfc5652">RFC 5652</see>,
        ///     describes an encapsulation syntax for data protection. It
        ///     is used to digitally sign, digest, authenticate, and/or encrypt
        ///     arbitrary message content.
        /// </remarks>
        public async Task<byte[]> CreateProtectedDataAsync(string keyName,
            byte[] plainText,
            CancellationToken cancel = default)
        {
            // Identify the recipient by the Subject Key ID.

            // TODO: Need a method to just the get BC public key
            // Get the BC key pair for the named key.
            var ekey = await Store.TryGetAsync(keyName, cancel).ConfigureAwait(false);
            if (ekey == null)
            {
                throw new KeyNotFoundException($"The key '{keyName}' does not exist.");
            }

            // TNA TODO
            /*
            AsymmetricCipherKeyPair kp = null;
            UseEncryptedKey(ekey, key => { kp = GetKeyPairFromPrivateKey(key); });

            // Add recipient type based on key type.
            var edGen = new CmsEnvelopedDataGenerator();
            switch (kp.Private)
            {
                case RsaPrivateCrtKeyParameters _:
                    edGen.AddKeyTransRecipient(kp.Public, Base58.Decode(ekey.Id));
                    break;
                case ECPrivateKeyParameters _:
                {
                    var cert = await CreateBcCertificateAsync(keyName, cancel).ConfigureAwait(false);
                    edGen.AddKeyAgreementRecipient(
                        CmsEnvelopedGenerator.ECDHSha1Kdf,
                        kp.Private,
                        kp.Public,
                        cert,
                        CmsEnvelopedGenerator.Aes256Wrap
                    );
                    break;
                }
                case Ed25519PrivateKeyParameters _:
                    var cert2 = await CreateBcCertificateAsync(keyName, cancel).ConfigureAwait(false);
                    edGen.AddKeyAgreementRecipient(
                        CmsEnvelopedGenerator.ECDHSha1Kdf,
                        kp.Private,
                        kp.Public,
                        cert2,
                        CmsEnvelopedGenerator.Aes256Wrap
                    );
                    break;
                default:
                {
                    throw new NotSupportedException($"The key type {kp.Private.GetType().Name} is not supported.");
                }
            }

            // Generate the protected data.
            var ed = edGen.Generate(
                new CmsProcessableByteArray(plainText),
                CmsEnvelopedGenerator.Aes256Cbc);
            return ed.GetEncoded();
            */
            return new byte[1];
        }

        /// <summary>
        ///     Decrypt CMS protected data.
        /// </summary>
        /// <param name="cipherText">
        ///     The protected CMS data.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     the plain text byte array of the protected data.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///     When the required private key, to decrypt the data, is not foumd.
        /// </exception>
        /// <remarks>
        ///     Cryptographic Message Syntax (CMS), aka PKCS #7 and
        ///     <see href="https://tools.ietf.org/html/rfc5652">RFC 5652</see>,
        ///     describes an encapsulation syntax for data protection. It
        ///     is used to digitally sign, digest, authenticate, and/or encrypt
        ///     arbitrary message content.
        /// </remarks>
        public async Task<byte[]> ReadProtectedDataAsync(byte[] cipherText,
            CancellationToken cancel = default)
        {
            var cms = new CmsEnvelopedDataParser(cipherText);

            // Find a recipient whose key we hold. We only deal with recipient names
            // issued by ipfs (O=ipfs, OU=keystore).
            var knownKeys = (await ListAsync(cancel).ConfigureAwait(false)).ToArray();
            var recipient = cms.GetRecipientInfos().GetRecipients()
               .OfType<RecipientInformation>()
               .Select(ri =>
                {
                    var kid = GetKeyId(ri);
                    var key = knownKeys.FirstOrDefault(k => k.Id == kid);
                    return new
                    {
                        recipient = ri, key
                    };
                }).FirstOrDefault(r => r.key != null);

            if (recipient == null)
            {
                throw new KeyNotFoundException("The required decryption key is missing.");
            }

            // Decrypt the contents.
            var decryptionKey = await GetPrivateKeyAsync(recipient.key.Name, cancel).ConfigureAwait(false);
            return recipient.recipient.GetContent(decryptionKey);
        }

        /// <summary>
        ///     Get the key ID for a recipient.
        /// </summary>
        /// <param name="ri">
        ///     A recepient of the message.
        /// </param>
        /// <returns>
        ///     The key ID of the recepient or <b>null</b> if the recepient info
        ///     is not understood or does not contain an IPFS key id.
        /// </returns>
        /// <remarks>
        ///     The key ID is either the Subject Key Identifier (preferred) or the
        ///     issuer's distinguished name with the form "CN=&lt;kid>,OU=keystore,O=ipfs".
        /// </remarks>
        private MultiHash GetKeyId(RecipientInformation ri)
        {
            // Any errors are simply ignored.
            try
            {
                // Subject Key Identifier is the key ID.
                if (ri.RecipientID.SubjectKeyIdentifier is { } ski)
                {
                    return new MultiHash(ski);
                }

                // Issuer is CN=<kid>,OU=keystore,O=ipfs
                var issuer = ri.RecipientID.Issuer;
                if (issuer != null && issuer.GetValueList(X509Name.OU).Contains("keystore")
                 && issuer.GetValueList(X509Name.O).Contains("ipfs"))
                {
                    var cn = issuer.GetValueList(X509Name.CN)[0] as string;
                    return new MultiHash(cn);
                }
            }
            catch (Exception e)
            {
                Log.Warn("Failed reading CMS recipient info.", e);
            }

            return null;
        }

        /// <summary>
        ///     Sets the passphrase for the key chain.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="UnauthorizedAccessException">
        ///     When the <paramref name="passphrase" /> is wrong.
        /// </exception>
        /// <remarks>
        ///     The <paramref name="passphrase" /> is used to generate a DEK (derived encryption
        ///     key).  The DEK is then used to encrypt the stored keys.
        ///     <para>
        ///         Neither the <paramref name="passphrase" /> nor the DEK are stored.
        ///     </para>
        /// </remarks>
        public async Task SetPassphraseAsync(SecureString passphrase,
            CancellationToken cancel = default)
        {
            // TODO: Verify DEK options.
            // TODO: get digest based on Options.Hash
            passphrase.UseSecretBytes(plain =>
            {
                var pdb = new Pkcs5S2ParametersGenerator(new Sha256Digest());
                pdb.Init(
                    plain,
                    Encoding.UTF8.GetBytes(Options.Dek.Salt),
                    Options.Dek.IterationCount);
                var key = (KeyParameter) pdb.GenerateDerivedMacParameters(Options.Dek.KeyLength * 8);
                _dek = key.GetKey().ToBase64NoPad().ToCharArray();
            });

            // Verify that that pass phrase is okay, by reading a key.
            var akey = await Store.TryGetAsync("self", cancel).ConfigureAwait(false);
            if (akey != null)
            {
                try
                {
                    UseEncryptedKey(akey, _ => { });
                }
                catch (Exception e)
                {
                    throw new UnauthorizedAccessException("The pass phrase is wrong.", e);
                }
            }

            Log.Debug("Pass phrase is okay");
        }

        /// <summary>
        ///     Find a key by its name.
        /// </summary>
        /// <param name="name">
        ///     The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     an <see cref="Microsoft.EntityFrameworkCore.Metadata.IKey" /> or <b>null</b> if the the key is not defined.
        /// </returns>
        public async Task<IKey> FindKeyByNameAsync(string name, CancellationToken cancel = default)
        {
            var key = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
            if (key == null)
            {
                return null;
            }

            return new KeyInfo
            {
                Id = key.Id, Name = key.Name
            };
        }

        /// <summary>
        ///     Gets the IPFS encoded public key for the specified key.
        /// </summary>
        /// <param name="name">
        ///     The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     the IPFS encoded public key.
        /// </returns>
        /// <remarks>
        ///     The IPFS public key is the base-64 encoding of a protobuf encoding containing
        ///     a type and the DER encoding of the PKCS Subject Public Key Info.
        /// </remarks>
        /// <seealso href="https://tools.ietf.org/html/rfc5280#section-4.1.2.7" />
        public async Task<string> GetPublicKeyAsync(string name, CancellationToken cancel = default)
        {
            // TODO: Rename to GetIpfsPublicKeyAsync
            string result = null;
            var ekey = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
            if (ekey != null)
            {
                UseEncryptedKey(ekey, key =>
                {
                    var kp = GetKeyPairFromPrivateKey(key);
                    var spki = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public).GetDerEncoded();

                    // Add protobuf cruft.
                    var publicKey = new PublicKey
                    {
                        Data = spki,
                        Type = kp.Public switch
                        {
                            RsaKeyParameters _ => KeyType.Rsa,
                            Ed25519PublicKeyParameters _ => KeyType.Ed25519,
                            ECPublicKeyParameters _ => KeyType.Secp256K1,
                            _ => throw new NotSupportedException(
                                $"The key type {kp.Public.GetType().Name} is not supported.")
                        }
                    };

                    using var ms = new MemoryStream();
                    Serializer.Serialize(ms, publicKey);
                    result = Convert.ToBase64String(ms.ToArray());
                });
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IKey> CreateAsync(string name,
            string keyType,
            int size,
            CancellationToken cancel = default)
        {
            // Apply defaults.
            if (string.IsNullOrWhiteSpace(keyType))
            {
                keyType = Options.DefaultKeyType;
            }

            if (size < 1)
            {
                size = Options.DefaultKeySize;
            }

            keyType = keyType.ToLowerInvariant();

            // Create the key pair.
            Log.DebugFormat("Creating {0} key named '{1}'", keyType, name);
            IAsymmetricCipherKeyPairGenerator g;
            switch (keyType)
            {
                case "rsa":
                    g = GeneratorUtilities.GetKeyPairGenerator("RSA");
                    g.Init(new RsaKeyGenerationParameters(
                        BigInteger.ValueOf(0x10001), new SecureRandom(), size, 25));
                    break;
                case "ed25519":
                    g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");
                    g.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
                    break;
                case "secp256k1":
                    g = GeneratorUtilities.GetKeyPairGenerator("EC");
                    g.Init(new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256k1, new SecureRandom()));
                    break;
                default:
                    throw new Exception($"Invalid key type '{keyType}'.");
            }

            var keyPair = g.GenerateKeyPair();
            Log.Debug("Created key");

            return await AddPrivateKeyAsync(name, keyPair, cancel).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> ExportAsync(string name,
            char[] password,
            CancellationToken cancel = default)
        {
            var pem = "";
            var key = await Store.GetAsync(name, cancel).ConfigureAwait(false);
            UseEncryptedKey(key, pkey =>
            {
                using var sw = new StringWriter();
                var pkcs8 = new Pkcs8Generator(pkey, Pkcs8Generator.PbeSha1_3DES)
                {
                    Password = password
                };
                var pw = new PemWriter(sw);
                pw.WriteObject(pkcs8);
                pw.Writer.Flush();
                pem = sw.ToString();
            });

            return pem;
        }

        /// <inheritdoc />
        public async Task<IKey> ImportAsync(string name,
            string pem,
            char[] password = null,
            CancellationToken cancel = default)
        {
            AsymmetricKeyParameter key;
            using (var sr = new StringReader(pem))
            {
                using var pf = new PasswordFinder
                {
                    Password = password
                };
                var reader = new PemReader(sr, pf);
                try
                {
                    key = reader.ReadObject() as AsymmetricKeyParameter;
                }
                catch (Exception e)
                {
                    throw new UnauthorizedAccessException("The password is wrong.", e);
                }

                if (key == null || !key.IsPrivate)
                {
                    throw new InvalidDataException("Not a valid PEM private key");
                }
            }

            return await AddPrivateKeyAsync(name, GetKeyPairFromPrivateKey(key), cancel).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default)
        {
            var keys = Store.Values.Select(key => (IKey) new KeyInfo
            {
                Id = key.Id, Name = key.Name
            });

            return Task.FromResult(keys);
        }

        /// <inheritdoc />
        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default)
        {
            var key = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
            if (key == null)
            {
                return null;
            }

            await Store.RemoveAsync(name, cancel).ConfigureAwait(false);
            return new KeyInfo
            {
                Id = key.Id, Name = key.Name
            };
        }

        /// <inheritdoc />
        public async Task<IKey> RenameAsync(string oldName,
            string newName,
            CancellationToken cancel = default)
        {
            var key = await Store.TryGetAsync(oldName, cancel).ConfigureAwait(false);
            if (key == null)
            {
                return null;
            }

            key.Name = newName;
            await Store.PutAsync(newName, key, cancel).ConfigureAwait(false);
            await Store.RemoveAsync(oldName, cancel).ConfigureAwait(false);

            return new KeyInfo
            {
                Id = key.Id, Name = newName
            };
        }

        /// <summary>
        ///     Gets the Bouncy Castle representation of the private key.
        /// </summary>
        /// <param name="name">
        ///     The local name of key.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's result is
        ///     the private key as an <b>AsymmetricKeyParameter</b>.
        /// </returns>
        public async Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string name,
            CancellationToken cancel = default)
        {
            var key = await Store.TryGetAsync(name, cancel).ConfigureAwait(false);
            if (key == null)
            {
                throw new KeyNotFoundException($"The key '{name}' does not exist.");
            }

            AsymmetricKeyParameter kp = null;
            UseEncryptedKey(key, pkey => { kp = pkey; });
            return kp;
        }

        private void UseEncryptedKey(EncryptedKey key, Action<AsymmetricKeyParameter> action)
        {
            using var sr = new StringReader(key.Pem);
            using var pf = new PasswordFinder
            {
                Password = _dek
            };
            var reader = new PemReader(sr, pf);
            var privateKey = (AsymmetricKeyParameter) reader.ReadObject();
            action(privateKey);
        }

        private async Task<IKey> AddPrivateKeyAsync(string name,
            AsymmetricCipherKeyPair keyPair,
            CancellationToken cancel)
        {
            // Create the key ID
            var keyId = CreateKeyId(keyPair.Public);

            // Create the PKCS #8 container for the key
            string pem;
            await using (var sw = new StringWriter())
            {
                var pkcs8 = new Pkcs8Generator(keyPair.Private, Pkcs8Generator.PbeSha1_3DES)
                {
                    Password = _dek
                };
                var pw = new PemWriter(sw);
                pw.WriteObject(pkcs8);
                await pw.Writer.FlushAsync();
                pem = sw.ToString();
            }

            // Store the key in the repository.
            var key = new EncryptedKey
            {
                Id = keyId.ToBase58(),
                Name = name,
                Pem = pem
            };
            await Store.PutAsync(name, key, cancel).ConfigureAwait(false);
            Log.DebugFormat("Added key '{0}' with ID {1}", name, keyId);

            return new KeyInfo
            {
                Id = key.Id, Name = key.Name
            };
        }

        /// <summary>
        ///     Create a key ID for the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks>
        ///     The key id is the SHA-256 multihash of its public key. The public key is
        ///     a protobuf encoding containing a type and
        ///     the DER encoding of the PKCS SubjectPublicKeyInfo.
        /// </remarks>
        private MultiHash CreateKeyId(AsymmetricKeyParameter key)
        {
            var spki = SubjectPublicKeyInfoFactory
               .CreateSubjectPublicKeyInfo(key)
               .GetDerEncoded();

            // Add protobuf cruft.
            var publicKey = new PublicKey
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

        private AsymmetricCipherKeyPair GetKeyPairFromPrivateKey(AsymmetricKeyParameter privateKey)
        {
            AsymmetricCipherKeyPair keyPair = null;
            switch (privateKey)
            {
                case RsaPrivateCrtKeyParameters rsa:
                {
                    var pub = new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent);
                    keyPair = new AsymmetricCipherKeyPair(pub, privateKey);
                    break;
                }
                case Ed25519PrivateKeyParameters ed:
                {
                    var pub = ed.GeneratePublicKey();
                    keyPair = new AsymmetricCipherKeyPair(pub, privateKey);
                    break;
                }
                case ECPrivateKeyParameters ec:
                {
                    var q = ec.Parameters.G.Multiply(ec.D);
                    var pub = new ECPublicKeyParameters(ec.AlgorithmName, q, ec.PublicKeyParamSet);
                    keyPair = new AsymmetricCipherKeyPair(pub, ec);
                    break;
                }
            }

            if (keyPair == null)
            {
                throw new NotSupportedException($"The key type {privateKey.GetType().Name} is not supported.");
            }

            return keyPair;
        }

        /// <summary>
        ///     Create a X509 certificate for the specified key.
        /// </summary>
        /// <param name="keyName">
        ///     The key name.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<byte[]> CreateCertificateAsync(string keyName,
            CancellationToken cancel = default)
        {
            var cert = await CreateBcCertificateAsync(keyName, cancel).ConfigureAwait(false);
            return cert.GetEncoded();
        }

        /// <summary>
        ///     Create a X509 certificate for the specified key.
        /// </summary>
        /// <param name="keyName">
        ///     The key name.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<X509Certificate> CreateBcCertificateAsync(string keyName,
            CancellationToken cancel = default)
        {
            // Get the BC key pair for the named key.
            var ekey = await Store.TryGetAsync(keyName, cancel).ConfigureAwait(false);
            if (ekey == null)
            {
                throw new KeyNotFoundException($"The key '{keyName}' does not exist.");
            }

            AsymmetricCipherKeyPair kp = null;
            UseEncryptedKey(ekey, key => { kp = GetKeyPairFromPrivateKey(key); });

            // A signer for the key.
            var ku = new KeyUsage(KeyUsage.DigitalSignature
              | KeyUsage.DataEncipherment
              | KeyUsage.KeyEncipherment);
            ISignatureFactory signatureFactory = null;
            switch (kp.Private)
            {
                case ECPrivateKeyParameters _:
                    signatureFactory = new Asn1SignatureFactory(
                        X9ObjectIdentifiers.ECDsaWithSha256.ToString(),
                        kp.Private);
                    break;
                case RsaPrivateCrtKeyParameters _:
                    signatureFactory = new Asn1SignatureFactory(
                        PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(),
                        kp.Private);
                    break;
                case Ed25519PrivateKeyParameters _:
                    signatureFactory = new Asn1SignatureFactory(
                        EdECObjectIdentifiers.id_Ed25519.Id,
                        kp.Private);
                    ku = new KeyUsage(KeyUsage.DigitalSignature);
                    break;
            }

            if (signatureFactory == null)
            {
                throw new NotSupportedException($"The key type {kp.Private.GetType().Name} is not supported.");
            }

            // Build the certificate.
            var dn = new X509Name($"CN={ekey.Id}, OU=keystore, O=ipfs");
            var ski = new SubjectKeyIdentifier(Base58.Decode(ekey.Id));

            // Not a certificate authority.
            // TODO: perhaps the "self" key is a CA and all other keys issued by it.
            var bc = new BasicConstraints(false);

            var certGenerator = new X509V3CertificateGenerator();
            certGenerator.SetIssuerDN(dn);
            certGenerator.SetSubjectDN(dn);
            certGenerator.SetSerialNumber(BigInteger.ValueOf(1));
            certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(10));
            certGenerator.SetNotBefore(DateTime.UtcNow);
            certGenerator.SetPublicKey(kp.Public);
            certGenerator.AddExtension(X509Extensions.SubjectKeyIdentifier, false, ski);
            certGenerator.AddExtension(X509Extensions.BasicConstraints, true, bc);
            certGenerator.AddExtension(X509Extensions.KeyUsage, false, ku);

            return certGenerator.Generate(signatureFactory);
        }

        private sealed class PasswordFinder : IPasswordFinder, IDisposable
        {
            public char[] Password;

            public void Dispose() { Password = null; }

            public char[] GetPassword() { return Password; }
        }
    }
}
