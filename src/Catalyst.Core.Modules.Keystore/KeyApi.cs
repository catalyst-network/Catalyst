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

using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Org.BouncyCastle.Crypto;

namespace Catalyst.Core.Modules.Keystore
{
    public sealed class KeyApi : IKeyApi
    {
        private readonly IKeyStoreService _keyStoreService;

        public KeyApi(IKeyStoreService keyStoreService)
        {
            _keyStoreService = keyStoreService;
        }

        public async Task<IKey> CreateAsync(KeyRegistryTypes keyRegistryType,
            string keyType,
            int size,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.CreateAsync(keyRegistryType.Name, keyType, size, cancel).ConfigureAwait(false);
        }

        public async Task<string> ExportAsync(KeyRegistryTypes keyRegistryType,
            char[] password,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.ExportAsync(keyRegistryType.Name, password, cancel).ConfigureAwait(false);
        }

        public async Task<IKey> ImportAsync(KeyRegistryTypes keyRegistryType, string pem, char[] password, CancellationToken cancel)
        {
            return await _keyStoreService.ImportAsync(keyRegistryType.Name, pem, password, cancel).ConfigureAwait(false);   
        }

        public async Task<IKey> GetKeyAsync(KeyRegistryTypes keyRegistryType)
        {
            return await _keyStoreService.FindKeyByNameAsync(keyRegistryType.Name).ConfigureAwait(false);
        }

        public async Task<string> GetIpfsPublicKeyAsync(KeyRegistryTypes keyRegistryType, CancellationToken cancel = default)
        {
            return await _keyStoreService.GetIpfsPublicKeyAsync(keyRegistryType.Name, cancel).ConfigureAwait(false);
        }

        public async Task<AsymmetricKeyParameter> GetPublicKeyAsync(KeyRegistryTypes keyRegistryType)
        {
            return await _keyStoreService.GetPublicKeyAsync(keyRegistryType.Name).ConfigureAwait(false);
        }

        public async Task<AsymmetricKeyParameter> GetPrivateKeyAsync(KeyRegistryTypes keyRegistryType)
        {
            return await _keyStoreService.GetPrivateKeyAsync(keyRegistryType.Name).ConfigureAwait(false);
        }

        public async Task<byte[]> CreateProtectedDataAsync(KeyRegistryTypes keyRegistryType,
            byte[] plainText,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.CreateProtectedDataAsync(keyRegistryType.Name, plainText, cancel);
        }

        public async Task<byte[]> ReadProtectedDataAsync(byte[] cipherText,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.ReadProtectedDataAsync(cipherText, cancel: cancel);
        }

        public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default)
        {
            return await _keyStoreService.ListAsync(cancel).ConfigureAwait(false);
        }

        public async Task<IKey> RemoveAsync(KeyRegistryTypes keyRegistryType, CancellationToken cancel = default)
        {
            return await _keyStoreService.RemoveAsync(keyRegistryType.Name, cancel).ConfigureAwait(false);
        }

        public async Task<IKey> RenameAsync(string oldName,
            string newName,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.RenameAsync(oldName, newName, cancel).ConfigureAwait(false);
        }

        public async Task SetPassphraseAsync(SecureString passphrase,
            CancellationToken cancel = default)
        {
            await _keyStoreService.SetPassphraseAsync(passphrase, cancel);
        }
    }
}
