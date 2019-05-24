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
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;
using Nethereum.RLP;

namespace Catalyst.Common.Modules.KeySigner
{
    public class KeySigner : IKeySigner
    {
        private readonly IKeyStore _keyStore;
        private readonly IUserOutput _userOutput;
        private readonly ICryptoContext _cryptoContext;
        private readonly IKeySignerInitializer _keySignerInitializer;
        private IPublicKey _publicKey;

        /// <summary>Initializes a new instance of the <see cref="KeySigner"/> class.</summary>
        /// <param name="userOutput">The user output.</param>
        /// <param name="keyStore">The key store.</param>
        /// <param name="cryptoContext">The crypto context.</param>
        /// <param name="initializer">The initializer.</param>
        public KeySigner(IUserOutput userOutput, IKeyStore keyStore, ICryptoContext cryptoContext, IKeySignerInitializer initializer)
        {
            _userOutput = userOutput;
            _keyStore = keyStore;
            _cryptoContext = cryptoContext;
            _keySignerInitializer = initializer;
        }

        /// <inheritdoc/>
        IKeyStore IKeySigner.KeyStore => _keyStore;

        /// <inheritdoc/>
        ICryptoContext IKeySigner.CryptoContext => _cryptoContext;

        /// <inheritdoc/>
        public ISignature Sign(byte[] data)
        {
            {
                IPrivateKey key = _keyStore.GetKey(Constants.DefaultKeyStoreFile, _keySignerInitializer.Password);
                return Task.FromResult(_cryptoContext.Sign(key, new ReadOnlySpan<byte>(data))).GetAwaiter().GetResult();
            }
        }

        /// <inheritdoc/>
        public bool Verify(AnySigned anySigned)
        {
            IPublicKey key = _cryptoContext.GetPublicKey(anySigned.PeerId.PublicKey.ToByteArray().ToStringFromRLPDecoded());
            byte[] payload = anySigned.Value.ToByteArray();
            var signature = new Signature(anySigned.Signature.ToByteArray());

            return _cryptoContext.Verify(key, payload, signature);
        }

        /// <inheritdoc/>
        public void ExportKey()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void ReadPassword()
        {
            _keySignerInitializer.ReadPassword(this);
        }

        /// <inheritdoc/>
        public void GenerateNewKey()
        {
            var newPrivateKey = _cryptoContext.GeneratePrivateKey();
            _keyStore.StoreKey(newPrivateKey, Constants.DefaultKeyStoreFile, _keySignerInitializer.Password);
            var publicKey = newPrivateKey.GetPublicKey();
            var publicKeyStr = _cryptoContext.AddressFromKey(publicKey);

            _userOutput.WriteLine("Generated new public key: "
              + publicKeyStr);
        }

        /// <inheritdoc/>
        public string GetPublicKey()
        {
            if (_publicKey == null)
            {
                _publicKey = _keyStore.GetKey(Constants.DefaultKeyStoreFile, _keySignerInitializer.Password).GetPublicKey();
            }

            return _cryptoContext.AddressFromKey(_publicKey);
        }
    }
}
