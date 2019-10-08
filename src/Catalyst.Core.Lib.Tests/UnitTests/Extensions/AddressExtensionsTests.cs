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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Account;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs.Types;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Extensions
{
    public class AddressExtensionsTests
    {
        [Theory]
        [InlineData("this string in bytes is way longer than 20 bytes so that makes for a good check", NetworkType.Mainnet, AccountType.PublicAccount)]
        [InlineData("short", NetworkType.Devnet, AccountType.ConfidentialAccount)]
        [InlineData("and another las one", NetworkType.Testnet, AccountType.SmartContractAccount)]
        public void Address_RawBytes_should_be_20_bytes_long_with_correct_network_and_account_types(string publicKeySeed, NetworkType networkType, AccountType accountType)
        {
            var address = publicKeySeed.ToUtf8Bytes()
               .ToAddress(networkType, accountType);

            address.RawBytes.Length.Should().Be(20);
            address.NetworkType.Should().Be(networkType);
            address.AccountType.Should().Be(accountType);
        }

        [Fact]
        public void AsBase32Crockford_should_produce_distinct_strings_for_all_account_types()
        {
            var allAccountTypes = TestUtils.Protocol.AddressHelper.GetAllNetworksAndAccountTypesCombinations();
            
            var base32Addresses = allAccountTypes.Select(a =>
                TestUtils.Protocol.AddressHelper.GetAddress("publicKey", a.NetworkType, a.AccountType)
                   .AsBase32Crockford());

            base32Addresses.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void ToEthereumAddress_should_understand_catalyst_public_key()
        {
            var publicKey = ByteUtil.GenerateRandomByteArray(32);
            var ethereumAddress = publicKey.ToEthereumAddress(NetworkType.Devnet, AccountType.PublicAccount);
            
            ethereumAddress.Bytes.Length.Should().Be(20);
        }
    }
}

