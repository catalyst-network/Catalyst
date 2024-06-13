#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

        public async Task<IKey> CreateAsync(string name,
            string keyType,
            int size,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.CreateAsync(name, keyType, size, cancel).ConfigureAwait(false);
        }

        public async Task<string> ExportAsync(string name,
            char[] password,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.ExportAsync(name, password, cancel).ConfigureAwait(false);
        }

        public async Task<IKey> ImportAsync(string name, string pem, char[] password, CancellationToken cancel)
        {
            return await _keyStoreService.ImportAsync(name, pem, password, cancel).ConfigureAwait(false);   
        }

        public async Task<IKey> GetPublicKeyAsync(string publicKeyName)
        {
            return await _keyStoreService.FindKeyByNameAsync(publicKeyName).ConfigureAwait(false);
        }

        public async Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string privateKeyName) => await _keyStoreService.GetPrivateKeyAsync(privateKeyName).ConfigureAwait(false);

        public async Task<byte[]> CreateProtectedDataAsync(string keyName,
            byte[] plainText,
            CancellationToken cancel = default)
        {
            return await _keyStoreService.CreateProtectedDataAsync(keyName, plainText, cancel);
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

        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default)
        {
            return await _keyStoreService.RemoveAsync(name, cancel).ConfigureAwait(false);
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
