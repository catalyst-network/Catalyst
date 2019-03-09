/*
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

using System;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;

namespace Catalyst.Node.Common.Modules.KeySigner
{
    public class KeySigner : IKeySigner
    {
        private readonly IKeyStore _keyStore;
        private readonly ICryptoContext _cryptoContext;

        public KeySigner(IKeyStore keyStore, ICryptoContext cryptoContext)
        {
            _keyStore = keyStore;
            _cryptoContext = cryptoContext;
        }

        IKeyStore IKeySigner.KeyStore => _keyStore;
        ICryptoContext IKeySigner.CryptoContext => _cryptoContext;
        
        public Task Sign(ReadOnlySpan<byte> data, string address, string password)
        {
            IPrivateKey key = _keyStore.GetKey(address, password);
            byte[] bytes = _cryptoContext.Sign(key, data);
            return Task.FromResult(new Signature(bytes));
        }

        public void Verify()
        {
            throw new NotImplementedException();
        }

        public void ExportKey()
        {
            throw new NotImplementedException();
        }
    }
}
