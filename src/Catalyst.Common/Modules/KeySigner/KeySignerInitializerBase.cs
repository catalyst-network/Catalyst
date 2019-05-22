using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Nethereum.KeyStore.Crypto;
using Nethereum.RLP;
using Serilog;

namespace Catalyst.Common.Modules.KeySigner
{
    public abstract class KeySignerInitializerBase : IKeySignerInitializer
    {
        private readonly IUserOutput _userOutput;
        
        private readonly ILogger _logger;

        private readonly IPasswordReader _passwordReader;

        private SecureString _password;

        protected readonly IKeyStore KeyStore;

        protected KeySignerInitializerBase(IPasswordReader passwordReader, IKeyStore keyStore, IUserOutput userOutput, ILogger logger)
        {
            KeyStore = keyStore;
            _userOutput = userOutput;
            _passwordReader = passwordReader;
            _logger = logger;
        }

        public string Password
        {
            get
            {
                if (_password == null)
                {
                    return string.Empty;
                }

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
            var path = Path.Combine(KeyStore.GetBaseDir(), Constants.DefaultKeyStoreFile);
            var keyStoreFileExists = File.Exists(path);

            try
            {
                if (keyStoreFileExists)
                {
                    IPublicKey publicKey =
                        KeyStore.GetKey(Constants.DefaultKeyStoreFile, Password).GetPublicKey();
                    var publicKeyStr = keySigner.CryptoContext.AddressFromKey(publicKey);
                    if (!publicKeyStr.Equals(
                        GetPeerIdentifier().PublicKey.ToStringFromRLPDecoded(), StringComparison.CurrentCultureIgnoreCase))
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

        public void GenerateNewKey(IKeySigner keySigner)
        {
            var newPrivateKey = keySigner.CryptoContext.GeneratePrivateKey();
            KeyStore.StoreKey(newPrivateKey, Constants.DefaultKeyStoreFile, Password);
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
