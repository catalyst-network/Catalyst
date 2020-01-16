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
using System.Security.Cryptography;

namespace MultiFormats.Cryptography
{
    internal class IdentityHash : HashAlgorithm
    {
        private byte[] _digest;

        public override void Initialize() { }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (_digest == null)
            {
                _digest = new byte[cbSize];
                Buffer.BlockCopy(array, ibStart, _digest, 0, cbSize);
                return;
            }

            var buffer = new byte[_digest.Length + cbSize];
            Buffer.BlockCopy(_digest, 0, buffer, _digest.Length, _digest.Length);
            Buffer.BlockCopy(array, ibStart, _digest, _digest.Length, cbSize);
            _digest = buffer;
        }

        protected override byte[] HashFinal() { return _digest; }
    }
}
