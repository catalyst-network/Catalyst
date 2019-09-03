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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Dawn;
using Nethereum.KeyStore;

namespace Catalyst.Core.Keystore
{
    public sealed class KeyStoreServiceWrapped : IKeyStoreService
    {
        private readonly KeyStoreService _keyStoreService;
        private readonly ICryptoContext _cryptoContext;

        public KeyStoreServiceWrapped(ICryptoContext cryptoContext)
        {
            _keyStoreService = new KeyStoreService();
            _cryptoContext = cryptoContext;
        }

        public string GetAddressFromKeyStore(string json)
        {
            return _keyStoreService.GetAddressFromKeyStore(json);
        }

        public string GenerateUtcFileName(string address)
        {
            return _keyStoreService.GenerateUTCFileName(address);
        }

        public byte[] DecryptKeyStoreFromJson(string password, string json)
        {
            return _keyStoreService.DecryptKeyStoreFromJson(password, json);
        }

        public string EncryptAndGenerateDefaultKeyStoreAsJson(string password, byte[] key, string address)
        {
            var pubKeyLen = _cryptoContext.PublicKeyLength;
            Guard.Argument(key, nameof(key)).MinCount(pubKeyLen).MaxCount(pubKeyLen);
            Guard.Argument(address, nameof(address)).NotEmpty().NotNull().NotWhiteSpace();
            Guard.Argument(password, nameof(password)).NotEmpty().NotNull().NotWhiteSpace();
            return _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password, key, address);
        }
    }
}
