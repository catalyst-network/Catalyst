#region LICENSE

/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Cryptography.BulletProofs.Wrapper <https://github.com/catalyst-network/Rust.Cryptography.FFI.Wrapper>
 *
 * Catalyst.Cryptography.BulletProofs.Wrapper is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Cryptography.BulletProofs.Wrapper is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Cryptography.BulletProofs.Wrapper If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Catalyst.Abstractions.Cryptography;

namespace Catalyst.Core.Modules.Cryptography.BulletProofs.Types
{
    public class PrivateKey : IPrivateKey
    {
        public byte[] Bytes { get; }

        internal PrivateKey(byte[] privateKey)
        { 
            var requiredLength = FFI.PublicKeyLength;
            if (privateKey.Length != requiredLength)
            {
                Error.ThrowArgumentExceptionPrivateKeyLength(requiredLength);
            }

            Bytes = privateKey;
        }

        public IPublicKey GetPublicKey()
        {
            return new PublicKey(FFI.GetPublicKeyFromPrivate(Bytes));
        }
    }
}
