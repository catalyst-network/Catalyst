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
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;
using Multiformats.Hash;

namespace Catalyst.Core.Lib.Extensions.Protocol.Account
{
    public static class AddressExtensions
    {
        public static Address ToAddress(this byte[] publicKey,
            NetworkType networkType,
            AccountType accountType)
        {
            var result = new Address
            {
                AccountType = accountType,
                NetworkType = networkType,
                PublicKeyHash = publicKey
                   .ComputeMultihash(AddressHelper.HashAlgorithm)
                   .Digest
                   .ToByteString()
            };
            return result;
        }

        public static Address ToAddress(this IPublicKey publicKey,
            NetworkType networkType,
            AccountType accountType)
        {
            return ToAddress(publicKey.Bytes, networkType, accountType);
        }

        public static string AsBase32Crockford(this Address address) =>
            SimpleBase.Base32.Crockford.Encode(address.RawBytes, false);

        /// <summary>
        /// Returns the 19 byte digest of the public key hash as a self
        /// describing multihash, ie including a description prefix
        /// </summary>
        /// <returns>Multihash compatible version of the PublicKeyHash for the address</returns>
        public static Multihash PublicKeyHashAsMultihash(this Address address)
        {
            return Multihash.Encode(address.PublicKeyHash.ToByteArray(), 
                AddressHelper.HashAlgorithm.Code);
        }
    }
}
