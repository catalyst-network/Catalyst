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
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;

namespace Catalyst.Common.Cryptography
{
    public sealed class RustCryptoContext : ICryptoContext
    {
        private static readonly IWrapper Wrapper = new CryptoWrapper();

        /// <inheritdoc />
        public IPrivateKey GeneratePrivateKey()
        {
            return Wrapper.GenerateKey();
        }

        /// <inheritdoc />
        public IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob)
        {
            return new PublicKey(blob.ToArray());
        }

        /// <inheritdoc />
        public byte[] ExportPublicKey(IPublicKey key)
        {
            return key.Bytes.RawBytes;
        }

        /// <inheritdoc />
        public IPrivateKey ImportPrivateKey(ReadOnlySpan<byte> blob)
        {
            return new PrivateKey(blob.ToArray());
        }

        /// <inheritdoc />
        public byte[] ExportPrivateKey(IPrivateKey key)
        {
            return key.Bytes.RawBytes;
        }

        /// <inheritdoc />
        public ISignature Sign(IPrivateKey privateKey, ReadOnlySpan<byte> data)
        {
            return Wrapper.StdSign(privateKey, data.ToArray());
        }
        
        /// <inheritdoc />
        public bool Verify(IPublicKey key, ReadOnlySpan<byte> message, ISignature signature)
        {
            return Wrapper.StdVerify(signature, key, message.ToArray());
        }

        /// <inheritdoc />
        public IPublicKey GetPublicKey(IPrivateKey key)
        {
            return Wrapper.GetPublicKeyFromPrivate(key);
        }
    }
}
