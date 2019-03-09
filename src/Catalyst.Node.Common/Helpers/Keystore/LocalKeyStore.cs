/*
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

using System;
using System.IO;
using Catalyst.Node.Common.Interfaces;
using Nethereum.KeyStore;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class LocalKeyStore : IKeyStore
    {
        private readonly ILogger _logger;
        public ICryptoContext CryptoContext { get; }
        private KeyStoreService KeyStoreService { get; }
        private FileSystem.FileSystem FileSystem { get; }

        public LocalKeyStore(ICryptoContext cryptoContext, ILogger logger)
        {
            CryptoContext = cryptoContext;
            _logger = logger; 
            KeyStoreService = new KeyStoreService();
            FileSystem  = new FileSystem.FileSystem();
        }
        
        public IPrivateKey GetKey(IPublicKey publicKey, string password) { throw new System.NotImplementedException(); }

        public IPrivateKey GetKey(string address, string password)
        {
            return CryptoContext.ImportPrivateKey(KeyStoreService.DecryptKeyStoreFromFile(password, address));
        }

        public bool StoreKey(IPrivateKey privateKey, string address, string password)
        {
            var json = KeyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password, CryptoContext.ExportPrivateKey(privateKey), address);

            try
            {
                using (var keyStoreFile = FileSystem.File.CreateText(address))
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
