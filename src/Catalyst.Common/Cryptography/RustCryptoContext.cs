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
using Cryptography.IWrapper.Types;
using Cryptography.IWrapper;

namespace Catalyst.Common.Cryptography
{
    public class RustCryptoContext : ICryptoContext
    {
        public IPrivateKey GeneratePrivateKey() { return ; }
        public IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob) { throw new NotImplementedException(); }
        public byte[] ExportPublicKey(IPublicKey key) { throw new NotImplementedException(); }
        public IPrivateKey ImportPrivateKey(ReadOnlySpan<byte> blob) { throw new NotImplementedException(); }
        public byte[] ExportPrivateKey(IPrivateKey key) { throw new NotImplementedException(); }
        public byte[] Sign(IPrivateKey privateKey, ReadOnlySpan<byte> data) { throw new NotImplementedException(); }
        public bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature) { throw new NotImplementedException(); }
        public IPublicKey GetPublicKey(IPrivateKey key) { throw new NotImplementedException(); }
        public string AddressFromKey(IPublicKey key) { throw new NotImplementedException(); }
    }
}
