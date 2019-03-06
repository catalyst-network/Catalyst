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

﻿using System.Collections.Generic;
using System.Linq;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Enumerator;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.P2P
{
    public static class NetworkTests
    {
        static NetworkTests()
        {
            NetworksAndExpectations = Enumeration.GetAll<Network>()
               .Select(n => new[] {n.Name, n as object}).ToList();
        }

        public static List<object[]> NetworksAndExpectations { get; set; }

        [Theory]
        [MemberData(nameof(NetworksAndExpectations))]
        public static void Network_can_be_parsed_from_string(string value, Network expectedNetwork)
        {
            var parsed = Enumeration.Parse<Network>(value);
            parsed.Should().Be(expectedNetwork);
        }

        [Fact]
        public static void All_should_return_all_declared_names()
        {
            var allModuleNames = Enumeration.GetAll<Network>().Select(m => m.Name);

            var expectedList = new List<string> {"mainnet", "devnet", "testnet"};

            allModuleNames.Should().BeEquivalentTo(expectedList);
        }
    }
}