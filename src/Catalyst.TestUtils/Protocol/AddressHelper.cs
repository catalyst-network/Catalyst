#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.Collections.Generic;
using System.Linq;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;
using MultiFormats.Registry;

namespace Catalyst.TestUtils.Protocol
{
    public static class AddressHelper
    {
        public static Address GetAddress(string publicKeySeed = "publicKey",
            NetworkType networkType = NetworkType.Devnet,
            AccountType accountType = AccountType.PublicAccount)
        {
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));

            var address = new Address
            {
                AccountType = accountType,
                NetworkType = networkType,
                PublicKeyHash = hashProvider.ComputeUtf8MultiHash(publicKeySeed).ToArray().ToByteString()
            };
            return address;
        }

        public class AddressType
        {
            public NetworkType NetworkType { get; }
            public AccountType AccountType { get; }

            public AddressType(NetworkType networkType, AccountType accountType)
            {
                NetworkType = networkType;
                AccountType = accountType;
            }
        }

        public static IEnumerable<AddressType> GetAllNetworksAndAccountTypesCombinations()
        {
            return from network in Enum.GetNames(typeof(NetworkType))
                from account in Enum.GetNames(typeof(AccountType))
                select new AddressType(Enum.Parse<NetworkType>(network),
                    Enum.Parse<AccountType>(account));
        }
    }
}
