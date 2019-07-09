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
using Catalyst.Common.Interfaces.Cryptography;
using System.IO;
using System.Linq;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Nethereum.KeyStore.Crypto;
using Serilog;

namespace Catalyst.Common.Keystore
{
    public sealed class LocalKeyStore : IKeyStore
    {
        private readonly ILogger _logger;
        private readonly IAddressHelper _addressHelper;
        private readonly IFileSystem _fileSystem;
        private readonly ICryptoContext _cryptoContext;
        private readonly IPasswordReader _passwordReader;
        private readonly IKeyStoreService _keyStoreService;
        private readonly PasswordRegistryKey _defaultNodePassword;

        private static int MaxTries => 5;

        public LocalKeyStore(IPasswordReader passwordReader,
            ICryptoContext cryptoContext,
            IKeyStoreService keyStoreService,
            IFileSystem fileSystem,
            ILogger logger,
            IAddressHelper addressHelper)
        {
            _passwordReader = passwordReader;
            _cryptoContext = cryptoContext;
            _keyStoreService = keyStoreService;
            _logger = logger;
            _addressHelper = addressHelper;
            _keyStoreService = keyStoreService;
            _fileSystem = fileSystem;
        }

        public IPrivateKey KeyStoreDecrypt(KeyRegistryKey keyIdentifier)
        {
            string json = GetJsonFromKeyStore(keyIdentifier);
            if (json == null) return null;
            var keyBytes = KeyStoreDecrypt(_defaultNodePassword, json);
            IPrivateKey privateKey = null;
            try
            {
                privateKey = _cryptoContext.ImportPrivateKey(keyBytes);
            }
            catch (ArgumentException)
            {
                _logger.Error("Keystore did not contain a valid key");
            }

            return privateKey;
        }

        private byte[] KeyStoreDecrypt(PasswordRegistryKey passwordIdentifier, string json)
        {
            var tries = 0;

            while (tries < MaxTries)
            {
                var securePassword = _passwordReader.ReadSecurePassword(passwordIdentifier);
                var password = StringFromSecureString(securePassword);
                
                try
                {
                    var keyStore = _keyStoreService.DecryptKeyStoreFromJson(password, json);
                    
                    if (keyStore != null && keyStore.Length > 0)
                    {
                        return keyStore;
                    }
                }
                catch (DecryptionException)
                {
                    _logger.Error("Error decrypting keystore");
                }

                tries += 1;
            }

            _logger.Error("Password incorrect for keystore.");
            return null;
        }
        
        public async Task<string> KeyStoreGenerateAsync(IPrivateKey privateKey, KeyRegistryKey keyIdentifier)
        {
            var address = _addressHelper.GenerateAddress(privateKey.GetPublicKey());        
            var securePassword = _passwordReader.ReadSecurePassword(_defaultNodePassword);

            var password = StringFromSecureString(securePassword);

            var json = _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(
                password: password, 
                key: _cryptoContext.ExportPrivateKey(privateKey),
                address: address);
            
            try
            {
                await _fileSystem.WriteFileToCddSubDirectoryAsync(keyIdentifier.Name, Constants.KeyStoreDataSubDir, json);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                throw;
            }
            
            return json;
        }

        private string GetJsonFromKeyStore(KeyRegistryKey keyIdentifier) 
        {
            if (keyIdentifier == KeyRegistryKey.DefaultKey)
            {
                return GetDefaultJsonFile();
            }
            else throw new Exception("Behaviour only defined for default key");
        }

        private string GetDefaultJsonFile()
        {
            var directoryInfo = _fileSystem.GetCatalystDataDir().SubDirectoryInfo(Constants.KeyStoreDataSubDir);
            if (!directoryInfo.Exists)
            {
                return null;
            }

            FileInfo keyStoreFile = directoryInfo.GetFiles().FirstOrDefault();

            if (keyStoreFile != null && keyStoreFile.Exists)
            {
                return File.ReadAllText(keyStoreFile.FullName);
            }

            _logger.Error("No keystore exists for the given key");
            return null;
        }

        private string StringFromSecureString(SecureString secureString)
        {
            var stringPointer = Marshal.SecureStringToBSTR(secureString);
            var password = Marshal.PtrToStringBSTR(stringPointer);
            Marshal.ZeroFreeBSTR(stringPointer);

            return password;
        }
    }
}
