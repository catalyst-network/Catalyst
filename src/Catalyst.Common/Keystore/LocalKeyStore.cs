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
    public sealed class LocalKeyStore : IKeyStore, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IAddressHelper _addressHelper;
        private readonly IFileSystem _fileSystem;
        private readonly ICryptoContext _cryptoContext;
        private readonly IPasswordReader _passwordReader;
        private readonly IKeyStoreService _keyStoreService;

        private SecureString _password;
        private static int MaxTries => 5;

        public string Password
        {
            get
            {
                if (_password == null)
                {
                    return string.Empty;
                }

                var stringPointer = Marshal.SecureStringToBSTR(_password);
                var normalString = Marshal.PtrToStringBSTR(stringPointer);
                Marshal.ZeroFreeBSTR(stringPointer);
                return normalString;
            }
        }
        
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

        public IPrivateKey KeyStoreDecrypt(string identifier)
        {
            string password = " password retrieval not implemented yet";
            string filepath = GetKeyFilePath(identifier);
            
            try
            {
                var keyBytes = _keyStoreService.DecryptKeyStoreFromJson(identifier, filename);
                    
                if (keyBytes != null && keyBytes.Length > 0)
                {
                    return new PrivateKey(keyBytes);
                }
            }
            catch (DecryptionException)
            {
                _logger.Error("Error decrypting keystore");
            }

            return null;
        }

        private string FilenameFromIdentifier(string identifier) { return identifier; }

        //need to change so keystore uses identifier to retrieve password
        public byte[] KeyStoreDecrypt(string identifier, string json)
        {
            var tries = 0;

            while (tries < MaxTries)
            {
                _password = _passwordReader.ReadSecurePassword("Please enter key signer password");

                try
                {
                    var keyStore = _keyStoreService.DecryptKeyStoreFromJson(Password, json);
                    
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

            throw new AuthenticationException("Password incorrect for keystore.");
        }
        
        public async Task<string> KeyStoreGenerateAsync(IPrivateKey privateKey, string password)
        {
            var address = _addressHelper.GenerateAddress(privateKey.GetPublicKey());
            
            var json = _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(
                password: password, 
                key: _cryptoContext.ExportPrivateKey(privateKey),
                address: address);
            
            try
            {
                await _fileSystem.WriteFileToCddAsync(_keyStoreService.GenerateUTCFileName(address), json);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                throw;
            }
            
            return json;
        }

        public IPrivateKey GetKey(string identifier)
        {
            var directoryInfo = _fileSystem.GetCatalystDataDir().SubDirectoryInfo(Constants.KeyStoreDataSubDir);
            if (!directoryInfo.Exists)
            {
                return null;
            }

            FileInfo keyStoreFile = directoryInfo.GetFiles("*.json").FirstOrDefault();
            return keyStoreFile.Exists ? new PrivateKey(KeyStoreDecrypt(Password, keyStoreFile.FullName)) : null;
        }

        public string GetKeyFilePath(string identifier)
        {
            var directoryInfo = _fileSystem.GetCatalystDataDir().SubDirectoryInfo(Constants.KeyStoreDataSubDir);
            if (!directoryInfo.Exists)
            {
                return null;
            }

            FileInfo keyStoreFile = directoryInfo.GetFiles("*.json").FirstOrDefault();
            return keyStoreFile.Exists ? keyStoreFile.FullName : "";
        }


        public void Dispose()
        {
            _password?.Dispose();
        }
    }
}
