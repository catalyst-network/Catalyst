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
            CancellationToken cancel = default(CancellationToken))
        {
            return await _keyStoreService.CreateAsync(name, keyType, size, cancel).ConfigureAwait(false);
        }

        public async Task<string> ExportAsync(string name,
            char[] password,
            CancellationToken cancel = default(CancellationToken))
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

        public async Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string privateKeyName)
        {
            return await _keyStoreService.GetPrivateKeyAsync(privateKeyName).ConfigureAwait(false);
        }

        public async Task<byte[]> CreateProtectedDataAsync(string keyName,
            byte[] plainText,
            CancellationToken cancel = default(CancellationToken))
        {
            return await _keyStoreService.CreateProtectedDataAsync(keyName, plainText, cancel);
        }

        public async Task<byte[]> ReadProtectedDataAsync(byte[] cipherText,
            CancellationToken cancel = default(CancellationToken))
        {
            return await _keyStoreService.ReadProtectedDataAsync(cipherText, cancel: cancel);
        }

        public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            return await _keyStoreService.ListAsync(cancel).ConfigureAwait(false);
        }

        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            return await _keyStoreService.RemoveAsync(name, cancel).ConfigureAwait(false);
        }

        public async Task<IKey> RenameAsync(string oldName,
            string newName,
            CancellationToken cancel = default(CancellationToken))
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
