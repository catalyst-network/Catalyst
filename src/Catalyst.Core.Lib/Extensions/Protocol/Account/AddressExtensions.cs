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
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Core.Lib.Extensions.Protocol.Account
{
    public static class AddressExtensions
    {
        public static readonly BLAKE2B_152 Blake2B152 = new BLAKE2B_152();

        public static Address ToAddress(this IPublicKey publicKey,
            NetworkType networkType,
            AccountType accountType)
        {
            var result = new Address
            {
                AccountType = accountType,
                NetworkType = networkType,
                PublicKeyHash = publicKey.Bytes
                   .ComputeRawHash(Blake2B152)
                   .ToByteString()
            };
            return result;
        }

        public static string AsBase32Crockford(this Address address) => 
            address.RawBytes.AsBase32Address();
    }
}
