using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Network;
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
        private readonly IDns _dns;
        private readonly ILookupClient _lookupClient;
        private readonly IPEndPoint _ipEndPoint;

        public DnsUnitTests()
        {
            _lookupClient = Substitute.For<ILookupClient>();
            _ipEndPoint = new IPEndPoint(IPAddress.Parse("9.9.9.9"), 53);
            _dns = new Dns(_lookupClient);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetTxtRecords_should_return_seeds_for_realz()
        {
            var trueClient = new LookupClient(_ipEndPoint);
            Dns dns = new Dns(trueClient);
            var dnsQueryResponse =
                await dns.GetTxtRecords("seed1.network.atlascity.io");
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            seedIp.Should().Be("92.207.178.198:42069");
        }

        [Fact]
        public async Task Dns_GetTxtRecords_should_return_IDnsQueryResponse_from_lookup_client()
        {
            
            var seed = "hoy";
            var value = "hey";
            var domainName = "domain.com"; 

            CreateFakeLookupResult(domainName, seed, value);

            var txtRecords = await _dns.GetTxtRecords(domainName);

            txtRecords.Should().BeAssignableTo<IDnsQueryResponse>();
            txtRecords.Answers.Count.Should().Be(1);
            txtRecords.Answers.First().DomainName.Value.Should().Be($"{domainName}.");
            ((TxtRecord) txtRecords.Answers.First()).EscapedText.Should().BeEquivalentTo(seed);
            ((TxtRecord) txtRecords.Answers.First()).Text.Should().BeEquivalentTo(value);
        }

        private void CreateFakeLookupResult(string domainName, string seed, string value)
        {
            var queryResponse = Substitute.For<IDnsQueryResponse>();
            var answers = new List<DnsResourceRecord>
            {
                new TxtRecord(new ResourceRecordInfo(domainName, ResourceRecordType.TXT, QueryClass.CS, 10, 32),
                    new[] {seed}, new[] {value})
            };

            queryResponse.Answers.Returns(answers);
            _lookupClient.QueryAsync(Arg.Is<string>(domainName), Arg.Any<QueryType>())
               .Returns(Task.FromResult(queryResponse));
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
        public async Task Dns_GetTxtRecords_from_list_should_return_IDnsQueryResponse_for_valid_list_of_strings_param()
        {
            var urlList = new List<string>();
            var domain1 = "seed1.network.atlascity.io";
            var domain2 = "seed2.network.atlascity.io";
            urlList.Add(domain1);
            urlList.Add(domain2);

            CreateFakeLookupResult(domain1, "seed1", "value1");
            CreateFakeLookupResult(domain2, "seed2", "value2");

            var responses = await _dns.GetTxtRecords(urlList);

            responses.Count.Should().Be(2);
            responses.Should().Contain(r => r.Answers[0].DomainName.Value.StartsWith(domain1));
            responses.Should().Contain(r => r.Answers[0].DomainName.Value.StartsWith(domain2));
        }

        [Fact]
        public async Task Dns_GetTxtRecords_from_list_should_return_IDnsQueryResponse_for_valid_list_of_strings_param_even_when_one_lookup_is_null()
        {
            var urlList = new List<string>();
            var domain1 = "seed1.network.atlascity.io";
            var domain2 = "seed2.network.atlascity.io";
            urlList.Add(domain1);
            urlList.Add(domain2);
            var queryResponse1 = Substitute.For<IDnsQueryResponse>();
            var queryResponse2 = Substitute.For<IDnsQueryResponse>();

            CreateFakeLookupResult(domain1, "seed1", "value1");

            _lookupClient.QueryAsync(Arg.Is<string>(domain2), Arg.Any<QueryType>())
               .Throws(new InvalidOperationException("failed"));

            var responses = await _dns.GetTxtRecords(urlList);

            responses.Count.Should().Be(1);
            responses.Should().Contain(r => r.Answers[0].DomainName.Value.StartsWith(domain1));
            responses.Should().NotContainNulls();
        }
    }
}