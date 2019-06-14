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

using Catalyst.Common.Interfaces.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Common.Util
{
    public sealed class AddressHelper : IAddressHelper
    {
        private IMultihashAlgorithm _hashAlgorithm;
        
        public void SetHashAlgorithm(IMultihashAlgorithm hashAlgorithm)
        {
            _hashAlgorithm = hashAlgorithm;

        }

        /// <summary>
        ///     returns a 20byte
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public string GenerateAddress(IPublicKey publicKey)
        {
            var addressHashBytes = _hashAlgorithm.ComputeHash(publicKey.Bytes.RawBytes);
            var lastTwentyBytes = ByteUtil.Slice(addressHashBytes, 12, 32);
            return lastTwentyBytes.ToHex();
        }
    }
}
