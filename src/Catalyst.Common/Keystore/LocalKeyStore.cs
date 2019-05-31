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
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Util;
using Nethereum.KeyStore.Crypto;
using Serilog;

namespace Catalyst.Common.KeyStore
{
    public sealed class LocalKeyStore : IKeyStore
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
        
        public byte[] KeyStoreDecrypt(string password, string json)
        {
            var tries = 0;

            while (tries < MaxTries)
            {
                _password = _passwordReader.ReadSecurePassword("Please enter key signer password");

                try
                {
                    return _keyStoreService.DecryptKeyStoreFromJson(Password, json);
                }
                catch (DecryptionException)
                {
                    _logger.Error("Error decrypting keystore");
                }

                tries += 1;
            }

            throw new InvalidOperationException("Failed to decrypt key signer, exiting");
        }
        
        public async Task<string> KeyStoreGenerate(IPrivateKey privateKey, string password)
        {
            var address = _addressHelper.GenerateAddress(privateKey.GetPublicKey());
            
            var json = _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(
                password: password, 
                key: _cryptoContext.ExportPrivateKey(privateKey),
                address: address);
            
            try
            {
                await _fileSystem.WriteFileToCDD(_keyStoreService.GenerateUTCFileName(address), json);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                throw;
            }
            
            return json;
        }
    }
}
