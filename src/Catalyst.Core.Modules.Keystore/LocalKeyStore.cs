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

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Enumerator;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;
using Nethereum.KeyStore;
using Nethereum.KeyStore.Crypto;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiBase;

namespace Catalyst.Core.Modules.Keystore
{
    public sealed class LocalKeyStore : KeyStoreService, IKeyStore
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ICryptoContext _cryptoContext;
        private readonly IPasswordManager _passwordManager;
        private readonly IHashProvider _hashProvider;
        private readonly PasswordRegistryTypes _defaultNodePassword = PasswordRegistryTypes.DefaultNodePassword;

        private static int MaxTries => 5;

        public LocalKeyStore(IPasswordManager passwordManager,
            ICryptoContext cryptoContext,
            IFileSystem fileSystem,
            IHashProvider hashProvider,
            ILogger logger)
        {
            _passwordManager = passwordManager;
            _cryptoContext = cryptoContext;
            _fileSystem = fileSystem;
            _hashProvider = hashProvider;
            _logger = logger;
        }

        public IPrivateKey KeyStoreDecrypt(KeyRegistryTypes keyIdentifier)
        {
            var json = GetJsonFromKeyStore(keyIdentifier);
            if (string.IsNullOrEmpty(json))
            {
                _logger.Error("No keystore exists for the given key");
                return null;
            }

            var keyBytes = KeyStoreDecrypt(_defaultNodePassword, json);
            IPrivateKey privateKey = null;
            try
            {
                privateKey = _cryptoContext.GetPrivateKeyFromBytes(keyBytes);
            }
            catch (ArgumentException)
            {
                _logger.Error("Keystore did not contain a valid key");
            }

            return privateKey;
        }

        private byte[] KeyStoreDecrypt(PasswordRegistryTypes passwordIdentifier, string json)
        {
            var tries = 0;

            while (tries < MaxTries)
            {
                var securePassword =
                    _passwordManager.RetrieveOrPromptPassword(passwordIdentifier, "Please provide your node password");
                var password = StringFromSecureString(securePassword);

                try
                {
                    var keyBytes = DecryptKeyStoreFromJson(password, json);

                    if (keyBytes != null && keyBytes.Length > 0)
                    {
                        _passwordManager.AddPasswordToRegistry(passwordIdentifier, securePassword);
                        return keyBytes;
                    }
                }
                catch (DecryptionException)
                {
                    securePassword.Dispose();
                    _logger.Error("Error decrypting keystore");
                }

                tries += 1;
            }

            throw new AuthenticationException("Password incorrect for keystore.");
        }

        public async Task<IPrivateKey> KeyStoreGenerate(NetworkType networkType, KeyRegistryTypes keyIdentifier)
        {
            var privateKey = _cryptoContext.GeneratePrivateKey();

            await KeyStoreEncryptAsync(privateKey, networkType, keyIdentifier).ConfigureAwait(false);

            return privateKey;
        }

        public async Task KeyStoreEncryptAsync(IPrivateKey privateKey,
            NetworkType networkType,
            KeyRegistryTypes keyIdentifier)
        {
            try
            {
                var publicKeyHash = _hashProvider.ComputeMultiHash(privateKey.GetPublicKey().Bytes).ToArray()
                   .ToByteString();
                var address = new Address
                {
                    PublicKeyHash = publicKeyHash,
                    AccountType = AccountType.PublicAccount,
                    NetworkType = networkType
                };

                var securePassword = _passwordManager.RetrieveOrPromptPassword(_defaultNodePassword,
                    "Please create a password for this node");

                var password = StringFromSecureString(securePassword);

                var json = EncryptAndGenerateDefaultKeyStoreAsJson(
                    password,
                    _cryptoContext.ExportPrivateKey(privateKey),
                    address.RawBytes.ToBase32());

                _passwordManager.AddPasswordToRegistry(_defaultNodePassword, securePassword);

                await _fileSystem.WriteTextFileToCddSubDirectoryAsync(keyIdentifier.Name, Constants.KeyStoreDataSubDir,
                    json);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }
        }

        private string GetJsonFromKeyStore(IEnumeration keyIdentifier)
        {
            return _fileSystem.ReadTextFromCddSubDirectoryFile(keyIdentifier.Name, Constants.KeyStoreDataSubDir);
        }

        private static string StringFromSecureString(SecureString secureString)
        {
            var stringPointer = Marshal.SecureStringToBSTR(secureString);
            var password = Marshal.PtrToStringBSTR(stringPointer);
            Marshal.ZeroFreeBSTR(stringPointer);

            return password;
        }
    }
}
