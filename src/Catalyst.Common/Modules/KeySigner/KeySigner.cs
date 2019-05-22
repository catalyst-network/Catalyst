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
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.Modules.KeySigner
{
    public class KeySigner : IKeySigner
    {
        private readonly IKeyStore _keyStore;
        private readonly ICryptoContext _cryptoContext;
        private readonly IKeySignerInitializer _keySignerInitializer;

        public KeySigner(IKeyStore keyStore, ICryptoContext cryptoContext, IKeySignerInitializer initializer)
        {
            _keyStore = keyStore;
            _cryptoContext = cryptoContext;
            _keySignerInitializer = initializer;
        }

        IKeyStore IKeySigner.KeyStore => _keyStore;
        ICryptoContext IKeySigner.CryptoContext => _cryptoContext;

        public ISignature Sign(byte[] data, string address)
        {
            {
                IPrivateKey key = _keyStore.GetKey(address, _keySignerInitializer.Password);
                return Task.FromResult(_cryptoContext.Sign(key, new ReadOnlySpan<byte>(data))).GetAwaiter().GetResult();
            }
        }
        
        public bool Verify(AnySigned anySigned)
        {
            IPublicKey key = new PublicKey(anySigned.PeerId.PublicKey.ToByteArray());
            byte[] payload = anySigned.Value.ToByteArray();
            var signature = new Signature(anySigned.Signature.ToByteArray());
            return _cryptoContext.Verify(key, payload, signature);
        }

        public void ExportKey()
        {
            throw new NotImplementedException();
        }

        public void ReadPassword()
        {
            _keySignerInitializer.ReadPassword(this);
        }

        public void GenerateNewKey()
        {
            _keySignerInitializer.GenerateNewKey(this);
        }
    }
}
