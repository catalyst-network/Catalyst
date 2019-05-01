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
using Catalyst.Common.Interfaces.Cryptography;
using Cryptography.IWrapper.Interfaces;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.KeyStore;
using Serilog;

namespace Catalyst.Common.KeyStore
{
    public sealed class LocalKeyStore : IKeyStore
    {
        private readonly ICryptoContext _cryptoContext;
        private readonly IKeyStoreWrapper _keyStoreService;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public LocalKeyStore(ICryptoContext cryptoContext, IKeyStoreWrapper keyStoreService, IFileSystem fileSystem, ILogger logger)
        {
            _cryptoContext = cryptoContext;
            _keyStoreService = keyStoreService;
            _logger = logger;
            _keyStoreService = keyStoreService;
            _fileSystem = fileSystem;
        }

        public IPrivateKey GetKey(IPublicKey publicKey, string password) { throw new NotImplementedException(); }

        public IPrivateKey GetKey(string fileName, string password)
        {
            var fullFilePath = Path.Combine(_fileSystem.GetCatalystHomeDir().FullName, fileName);
            var decryptKeyStoreFromFile = _keyStoreService.DecryptKeyStoreFromFile(password, fullFilePath);
            return _cryptoContext.ImportPrivateKey(new ReadOnlySpan<byte>(decryptKeyStoreFromFile));
        }

        public bool StoreKey(IPrivateKey privateKey, string fileName, string password)
        {
            var json = _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password, _cryptoContext.ExportPrivateKey(privateKey), fileName);

            try
            {
                using (var keyStoreFile = File.CreateText(Path.Combine(_fileSystem.GetCatalystHomeDir().FullName, fileName)))
                {
                    keyStoreFile.Write(json);
                    keyStoreFile.Flush();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }
            
            return true;
        }
    }
}
