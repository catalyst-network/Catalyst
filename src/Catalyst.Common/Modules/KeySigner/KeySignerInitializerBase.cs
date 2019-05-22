using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Microsoft.Extensions.Configuration;
using Nethereum.KeyStore.Crypto;
using Nethereum.RLP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using Serilog;

namespace Catalyst.Common.KeyStore
{
    public abstract class KeySignerInitializerBase : IKeySignerInitializer
    {
        private readonly IPeerIdentifier _peerIdentifier;

        private readonly IUserOutput _userOutput;

        private readonly IKeyStore _keyStore;

        private readonly ILogger _logger;

        private readonly IPasswordReader _passwordReader;

        private SecureString _password;

        protected KeySignerInitializerBase(IPasswordReader passwordReader, IKeyStore keyStore, IUserOutput userOutput, ILogger logger)
        {
            _peerIdentifier = GetPeerIdentifier();
            _keyStore = keyStore;
            _userOutput = userOutput;
            _passwordReader = passwordReader;
            _logger = logger;
        }

        public string Password
        {
            get
            {
                IntPtr stringPointer = Marshal.SecureStringToBSTR(_password);
                string normalString = Marshal.PtrToStringBSTR(stringPointer);
                Marshal.ZeroFreeBSTR(stringPointer);
                return normalString;
            }
        }

        public void ReadPassword(IKeySigner keySigner)
        {
            bool success = false;

            while (!success)
            {
                _password = _passwordReader.ReadSecurePassword("Please enter key signer password");

                try
                {
                    Initialize(keySigner);
                    success = true;
                }
                catch (DecryptionException decryptionException)
                {
                    _logger.Error(decryptionException, "Error decrypting key signer IKeySignerInitializer.ReadPassword");
                }
            }
        }

        public void Initialize(IKeySigner keySigner)
        {
            var path = Path.Combine(_keyStore.GetBaseDir(), Constants.DefaultKeyStoreFile);
            var keyStoreFileExists = File.Exists(path);

            try
            {
                if (keyStoreFileExists)
                {
                    IPublicKey publicKey =
                        _keyStore.GetKey(Constants.DefaultKeyStoreFile, Password).GetPublicKey();
                    var publicKeyStr = keySigner.CryptoContext.AddressFromKey(publicKey);
                    if (!publicKeyStr.Equals(
                        _peerIdentifier.PublicKey.ToStringFromRLPDecoded(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        _userOutput.WriteLine("Shell.Config Public Key Configuration does not match keystore");
                        SetConfigurationValue(publicKeyStr);
                    }
                }
                else
                {
                    GenerateNewKey(keySigner);
                }
            }
            catch (DecryptionException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error on IKeySignerInitializer.Initialize");
                GenerateNewKey(keySigner);
            }
        }

        private void GenerateNewKey(IKeySigner keySigner)
        {
            var newPrivateKey = keySigner.CryptoContext.GeneratePrivateKey();
            _keyStore.StoreKey(newPrivateKey, Constants.DefaultKeyStoreFile, Password);
            var publicKey = newPrivateKey.GetPublicKey();
            var publicKeyStr = keySigner.CryptoContext.AddressFromKey(publicKey);

            SetConfigurationValue(publicKeyStr);

            _userOutput.WriteLine("Generated new public key: "
              + publicKeyStr);
        }

        public abstract void SetConfigurationValue(string publicKey);

        public abstract IPeerIdentifier GetPeerIdentifier();
    }
}
