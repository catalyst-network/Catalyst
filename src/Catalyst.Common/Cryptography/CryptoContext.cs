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
    public sealed class CryptoContext : ICryptoContext
    {
        private readonly IWrapper _wrapper;

        public CryptoContext(IWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        /// <inheritdoc />
        public IPrivateKey GeneratePrivateKey()
        {
            return _wrapper.GenerateKey();
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
            return _wrapper.StdSign(privateKey, data.ToArray());
        }

        /// <inheritdoc />
        public bool Verify(ISignature signature, ReadOnlySpan<byte> message)
        {
            // TODO implement key signer 
            return /*_wrapper.StdVerify(signature, message.ToArray())*/ true;
        }

        /// <inheritdoc />
        public IPublicKey GetPublicKey(IPrivateKey key)
        {
            return _wrapper.GetPublicKeyFromPrivate(key);
        }

        public int PrivateKeyLength => _wrapper.PrivateKeyLength;

        public int PublicKeyLength => _wrapper.PublicKeyLength;

        public int SignatureLength => _wrapper.SignatureLength;
    }
}
