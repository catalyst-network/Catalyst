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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Catalyst.Common.UnitTests.P2P
{
    public class NetworkTests
    {
        static NetworkTests()
        {
            NetworksAndExpectations = new[]
                {
                    Protocol.Common.Network.Devnet, 
                    Protocol.Common.Network.Mainnet, 
                    Protocol.Common.Network.Testnet
                }.Select(n => new object[] {n.ToString(), n})
               .ToList();
        }

        public static List<object[]> NetworksAndExpectations;

        [Theory]
        [MemberData(nameof(NetworksAndExpectations))]
        public static void Network_can_be_parsed_from_string(string value, Protocol.Common.Network expectedNetwork)
        {
            Enum.TryParse(value, out Protocol.Common.Network parsed);
            parsed.Should().Be(expectedNetwork);
        }
    }
}
