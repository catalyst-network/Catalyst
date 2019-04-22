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

using Catalyst.Common.Interfaces.KeyStore;
using Nethereum.KeyStore;

namespace Catalyst.Common.KeyStore
{
    public class KeyStoreServiceWrapped : IKeyStoreWrapper
    {
        private readonly KeyStoreService _keyStoreWrapperImplementation;
        
        public KeyStoreServiceWrapped()
        {
            _keyStoreWrapperImplementation = new KeyStoreService();
        }

        public string GetAddressFromKeyStore(string json)
        {
            return _keyStoreWrapperImplementation.GetAddressFromKeyStore(json);
        }

        public string GenerateUTCFileName(string address)
        {
            return _keyStoreWrapperImplementation.GenerateUTCFileName(address);
        }

        public byte[] DecryptKeyStoreFromFile(string password, string filePath)
        {
            return _keyStoreWrapperImplementation.DecryptKeyStoreFromFile(password, filePath);
        }

        public byte[] DecryptKeyStoreFromJson(string password, string json)
        {
            return _keyStoreWrapperImplementation.DecryptKeyStoreFromJson(password, json);
        }

        public string EncryptAndGenerateDefaultKeyStoreAsJson(string password, byte[] key, string address)
        {
            return _keyStoreWrapperImplementation.EncryptAndGenerateDefaultKeyStoreAsJson(password, key, address);
        }
    }
}
