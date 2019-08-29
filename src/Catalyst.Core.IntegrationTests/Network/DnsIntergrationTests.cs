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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using Catalyst.TestUtils;
using DnsClient;
using DnsClient.Protocol;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.IntegrationTests.Network
{
    public sealed class DnsIntegrationTests
    {
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetTxtRecords_should_return_seeds()
        {
            var trueClient = new LookupClient(new IPEndPoint(IPAddress.Parse("9.9.9.9"), 53));
            var dns = new Core.Network.DnsClient(trueClient, Substitute.For<IPeerIdValidator>());
            var dnsQueryResponse =
                await dns.GetTxtRecordsAsync("seed1.network.atlascity.io");
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            seedIp.Should().Be("0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839");
        }
    }
}
