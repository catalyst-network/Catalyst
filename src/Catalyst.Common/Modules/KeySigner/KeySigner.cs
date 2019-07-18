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
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;

namespace Catalyst.Common.Modules.KeySigner
{
    public class KeySigner : IKeySigner
    {
        private readonly IKeyStore _keyStore;
        private readonly ICryptoContext _cryptoContext;

        /// <summary>Initializes a new instance of the <see cref="KeySigner"/> class.</summary>
        /// <param name="keyStore">The key store.</param>
        /// <param name="cryptoContext">The crypto context.</param>
        public KeySigner(IKeyStore keyStore,
            ICryptoContext cryptoContext)
        {
            _keyStore = keyStore;
            _cryptoContext = cryptoContext;
        }

        /// <inheritdoc/>
        IKeyStore IKeySigner.KeyStore => _keyStore;

        /// <inheritdoc/>
        ICryptoContext IKeySigner.CryptoContext => _cryptoContext;

        /// <inheritdoc/>
        public ISignature Sign(byte[] data)
        {
            {
                // var key = _keyStore.KeyStoreDecrypt(_keyStore.Password);
                // return Task.FromResult(_cryptoContext.Sign(key, new ReadOnlySpan<byte>(data))).GetAwaiter().GetResult();
            }
            return new Signature(new byte[64], new byte[32]);
        }

        /// <inheritdoc/>
        public virtual bool Verify(ISignature signature, byte[] message)
        {          
            return _cryptoContext.Verify(signature, message);
        }

        /// <inheritdoc/>
        public void ExportKey()
        {
            throw new NotImplementedException();
        }    
    }
}
