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

using System.Linq;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Extensions;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Core.Util
{
    public sealed class AddressHelper : IAddressHelper
    {
        private readonly IMultihashAlgorithm _hashAlgorithm;

        public AddressHelper(IMultihashAlgorithm hashAlgorithm)
        {
            _hashAlgorithm = hashAlgorithm;
        }

        /// <summary>
        ///     Generates an address from the 20 last bytes of the hashed public key.
        /// </summary>
        /// <param name="publicKey">The public key from which the address is derived.</param>
        /// <returns>The Hex encoded bytes corresponding to the address.</returns>
        public string GenerateAddress(IPublicKey publicKey)
        {
            var addressHashBytes = publicKey.Bytes.ComputeRawHash(_hashAlgorithm);
            var lastTwentyBytes = addressHashBytes.TakeLast(20).ToArray();
            return lastTwentyBytes.ToHex();
        }
    }
}
