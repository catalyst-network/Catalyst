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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Protocol;
using Catalyst.Protocol.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;

namespace Catalyst.Core.Modules.KeySigner
{
    public sealed class KeySigner : IKeySigner
    {
        private readonly ICryptoContext _cryptoContext;
        private readonly IKeyApi _keyApi;
        private readonly IKeyRegistry _keyRegistry;

        /// <summary>Initializes a new instance of the <see cref="KeySigner"/> class.</summary>
        /// <param name="keyStore">The key store.</param>
        /// <param name="cryptoContext">The crypto context.</param>
        /// /// <param name="keyRegistry">The key registry.</param>
        public KeySigner(ICryptoContext cryptoContext,
            IKeyApi keyApi,
            IKeyRegistry keyRegistry)
        {
            _cryptoContext = cryptoContext;
            _keyApi = keyApi;
            _keyRegistry = keyRegistry;
        }

        /// <inheritdoc/>
        ICryptoContext IKeySigner.CryptoContext => _cryptoContext;

        public IPrivateKey GetPrivateKey(KeyRegistryTypes keyIdentifier)
        {
            if (_keyRegistry.RegistryContainsKey(keyIdentifier))
            {
                return _keyRegistry.GetItemFromRegistry(keyIdentifier);
            }

            var privateKeyParameters = (Ed25519PrivateKeyParameters) _keyApi.GetPrivateKeyAsync(keyIdentifier.Name).GetAwaiter().GetResult();
            var privateKeyBytes = privateKeyParameters.GetEncoded();
            var privateKey = _cryptoContext.GetPrivateKeyFromBytes(privateKeyBytes);

            return privateKey;
        }

        public ISignature Sign(ReadOnlySpan<byte> data, SigningContext signingContext)
        {
            var privateKey = GetPrivateKey(KeyRegistryTypes.DefaultKey);

            using var pooled = signingContext.SerializeToPooledBytes();

            var signature = _cryptoContext.Sign(privateKey, data, pooled.Span);

            return signature;
        }

        /// <inheritdoc/>
        public bool Verify(ISignature signature, ReadOnlySpan<byte> data, SigningContext signingContext)
        {
            using var pooled = signingContext.SerializeToPooledBytes();

            return _cryptoContext.Verify(signature, data, pooled.Span);
        }
    }
}
