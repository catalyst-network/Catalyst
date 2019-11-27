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
using System.Buffers;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Protocol;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Google.Protobuf;
using Org.BouncyCastle.Security;

namespace Catalyst.Core.Modules.KeySigner
{
    public sealed class KeySigner : IKeySigner
    {
        private readonly IKeyStore _keyStore;
        private readonly ICryptoContext _cryptoContext;
        private readonly IKeyRegistry _keyRegistry;
        private readonly KeyRegistryTypes _defaultKey = KeyRegistryTypes.DefaultKey;
        static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

        /// <summary>Initializes a new instance of the <see cref="KeySigner"/> class.</summary>
        /// <param name="keyStore">The key store.</param>
        /// <param name="cryptoContext">The crypto context.</param>
        /// /// <param name="keyRegistry">The key registry.</param>
        public KeySigner(IKeyStore keyStore, 
            ICryptoContext cryptoContext, 
            IKeyRegistry keyRegistry)
        {
            _keyStore = keyStore;
            _cryptoContext = cryptoContext;
            _keyRegistry = keyRegistry;
            InitialiseKeyRegistry();
        }

        private void InitialiseKeyRegistry()
        {
            if (!TryPopulateDefaultKeyFromKeyStore(out _))
            {
                GenerateKeyAndPopulateRegistryWithDefaultAsync().ConfigureAwait(false);
            }   
        }

        private async Task GenerateKeyAndPopulateRegistryWithDefaultAsync()
        {
            var privateKey = await _keyStore.KeyStoreGenerateAsync(NetworkType.Devnet, _defaultKey).ConfigureAwait(false);
            if (privateKey != null)
            { 
                _keyRegistry.AddItemToRegistry(_defaultKey, privateKey);
            }
        }

        /// <inheritdoc/>
        IKeyStore IKeySigner.KeyStore => _keyStore;

        /// <inheritdoc/>
        ICryptoContext IKeySigner.CryptoContext => _cryptoContext;

        IPrivateKey GetPrivateKey(KeyRegistryTypes keyIdentifier)
        {
            var privateKey = _keyRegistry.GetItemFromRegistry(keyIdentifier);
            if (privateKey == null && !TryPopulateRegistryFromKeyStore(keyIdentifier, out privateKey))
            {
                throw new SignatureException("The signature cannot be created because the key does not exist");
            }

            return privateKey;
        }

        public ISignature Sign(ReadOnlySpan<byte> data, SigningContext signingContext)
        {
            var privateKey = GetPrivateKey(KeyRegistryTypes.DefaultKey);

            var span = Pool.Serialize(signingContext, out var array);

            var result = _cryptoContext.Sign(privateKey, data, span);
            
            Pool.Return(array);
            return result;
        }

        /// <inheritdoc/>
        public bool Verify(ISignature signature, ReadOnlySpan<byte> data, SigningContext signingContext)
        {
            var span = Pool.Serialize(signingContext, out var array);
            
            var result = _cryptoContext.Verify(signature, data, span);
            
            Pool.Return(array);
            return result;
        }

        public void ExportKey()
        {
            throw new NotImplementedException();
        }

        private bool TryPopulateRegistryFromKeyStore(KeyRegistryTypes keyIdentifier, out IPrivateKey key)
        {
            key = _keyStore.KeyStoreDecrypt(keyIdentifier);
            
            return key != null && (_keyRegistry.RegistryContainsKey(keyIdentifier) || _keyRegistry.AddItemToRegistry(keyIdentifier, key));
        }

        private bool TryPopulateDefaultKeyFromKeyStore(out IPrivateKey key)
        {
            return TryPopulateRegistryFromKeyStore(_defaultKey, out key);
        }   
    }
}
