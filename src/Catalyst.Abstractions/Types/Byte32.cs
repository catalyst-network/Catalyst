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

namespace Catalyst.Abstractions.Types
{
    public class Byte32 : FixedSizeBytes<Byte32>
    {
        protected sealed override int Size => 32;

        public Byte32() : this(null) { }

        public Byte32(byte[] rawBytes)
        {
            RawBytes = rawBytes ?? new byte[Size];
        }
    }
}
