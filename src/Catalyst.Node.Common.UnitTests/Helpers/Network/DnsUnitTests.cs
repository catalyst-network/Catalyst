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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using DnsClient;
using DnsClient.Protocol;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Dns = Catalyst.Node.Common.Helpers.Network.Dns;

namespace Catalyst.Node.Common.UnitTests.Helpers.Network
{
    public class DnsUnitTests
    {
        public DnsUnitTests()
        {
            _lookupClient = Substitute.For<ILookupClient>();
            _ipEndPoint = new IPEndPoint(IPAddress.Parse("9.9.9.9"), 53);
            _dns = new Dns(_lookupClient);
        }

        private readonly IDns _dns;
        private readonly ILookupClient _lookupClient;
        private readonly IPEndPoint _ipEndPoint;

        [Fact]
        public async Task Dns_GetTxtRecords_from_list_should_return_IDnsQueryResponse_for_valid_list_of_strings_param()
        {
            var urlList = new List<string>();
            var domain1 = "seed1.catalystnetwork.io";
            var domain2 = "seed2.catalystnetwork.io";
            urlList.Add(domain1);
            urlList.Add(domain2);

            MockQueryResponse.CreateFakeLookupResult(domain1, "value1", _lookupClient);
            MockQueryResponse.CreateFakeLookupResult(domain2, "value2", _lookupClient);

            var responses = await _dns.GetTxtRecords(urlList);

            responses.Count.Should().Be(2);
            responses.Should().Contain(r => r.Answers[0].DomainName.Value.StartsWith(domain1));
            responses.Should().Contain(r => r.Answers[0].DomainName.Value.StartsWith(domain2));
        }

        [Fact]
        public async Task
            Dns_GetTxtRecords_from_list_should_return_IDnsQueryResponse_for_valid_list_of_strings_param_even_when_one_lookup_is_null()
        {
            var urlList = new List<string>();
            var domain1 = "seed1.catalystnetwork.io";
            var domain2 = "seed2.catalystnetwork.io";
            urlList.Add(domain1);
            urlList.Add(domain2);

            MockQueryResponse.CreateFakeLookupResult(domain1, "value1", _lookupClient);

            _lookupClient.QueryAsync(Arg.Is(domain2), Arg.Any<QueryType>())
               .Throws(new InvalidOperationException("failed"));

            var responses = await _dns.GetTxtRecords(urlList);

            responses.Count.Should().Be(1);
            responses.Should().Contain(r => r.Answers[0].DomainName.Value.StartsWith(domain1));
            responses.Should().NotContainNulls();
        }

        [Fact]
        public async Task Dns_GetTxtRecords_should_return_IDnsQueryResponse_from_lookup_client()
        {
            var value = "hey";
            var domainName = "domain.com";

            MockQueryResponse.CreateFakeLookupResult(domainName, value, _lookupClient);

            var txtRecords = await _dns.GetTxtRecords(domainName);

            txtRecords.Should().BeAssignableTo<IDnsQueryResponse>();
            txtRecords.Answers.Count.Should().Be(1);
            txtRecords.Answers.First().DomainName.Value.Should().Be($"{domainName}.");
            ((TxtRecord) txtRecords.Answers.First()).EscapedText.Should().BeEquivalentTo(value);
            ((TxtRecord) txtRecords.Answers.First()).Text.Should().BeEquivalentTo(value);
        }

        [Fact]
        public async Task Dns_GetTxtRecords_When_Lookup_Throws_should_return_null()
        {
            _lookupClient.QueryAsync(Arg.Any<string>(), Arg.Any<QueryType>())
               .Throws(new InvalidOperationException("failed"));

            var txtRecords = await _dns.GetTxtRecords("www.internet.com");

            txtRecords.Should().BeNull();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetTxtRecords_should_return_seeds_for_realz()
        {
            var trueClient = new LookupClient(_ipEndPoint);
            var dns = new Dns(trueClient);
            var dnsQueryResponse =
                await dns.GetTxtRecords("seed1.network.atlascity.io");
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            seedIp.Should().Be("92.207.178.198:42069");
        }
    }
}
