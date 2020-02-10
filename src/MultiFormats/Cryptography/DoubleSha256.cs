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
    internal class DoubleSha256 : HashAlgorithm
    {
        private HashAlgorithm _digest = SHA256.Create();
        private byte[] _round1;

        public override void Initialize()
        {
            _digest.Initialize();
            _round1 = null;
        }

        public override int HashSize => _digest.HashSize;

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (_round1 != null)
                throw new NotSupportedException("Already called.");

            _round1 = _digest.ComputeHash(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            _digest.Initialize();
            return _digest.ComputeHash(_round1);
        }
    }
}
