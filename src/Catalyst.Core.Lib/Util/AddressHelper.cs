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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Core.Lib.Util
{
    public sealed class AddressHelper : IAddressHelper
    {
        /// <summary>
        /// Using <see cref="BLAKE2B_152"/> for the default implementation; the intent is to get a
        /// 19 byte long hash, then is prefixed by one byte used to identify
        /// the <see cref="AccountType"/>|<see cref="NetworkType"/> for which this 
        /// address is valid, and get EVM compatible 20 bytes addresses as a result. 
        /// </summary>
        public static readonly IMultihashAlgorithm HashAlgorithm = new BLAKE2B_152();

        private readonly NetworkType _networkType;

        public AddressHelper(IPeerSettings peerSettings) : this(peerSettings.NetworkType) { }

        public AddressHelper(NetworkType networkType)
        {
            _networkType = networkType;
        }

        /// <inheritdoc />
        public Address GenerateAddress(IPublicKey publicKey, AccountType accountType)
        {
            var publicKeyHash = publicKey.Bytes.ComputeMultihash(HashAlgorithm).Digest.ToByteString();
            var address = new Address
            {
                PublicKeyHash = publicKeyHash,
                AccountType = accountType,
                NetworkType = _networkType
            };
            return address;
        }
    }
}
