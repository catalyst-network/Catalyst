using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Core.Helpers.Network;
using DnsClient;
using DnsClient.Protocol;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Dns = Catalyst.Node.Core.Helpers.Network.Dns;
using Catalyst.Node.Common.UnitTests.TestUtils;

namespace Catalyst.Node.Core.UnitTest.Helpers.Network
{
    public class DnsUnitTests
    {
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task MethodTest()
        {
            Dns dns = new Dns(new IPEndPoint(IPAddress.Parse("9.9.9.9"), 53));
            var dnsQueryResponse =
                await dns.GetTxtRecords("seed1.network.atlascity.io");
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            seedIp.Should().Be("92.207.178.198:42069");
        }
        
        [Fact]
        public void Dns_GetTxtRecords_should_return_IDnsQueryResponse_for_valid_string_param()
        {
            var getTxtRecords = Substitute.For<IDns>().GetTxtRecords("seed1.network.atlascity.io");
            getTxtRecords.Should()
               .BeOfType<Task<IDnsQueryResponse>>();
        }
        
        [Fact]
        public void Dns_GetTxtRecords_should_return_IDnsQueryResponse_for_valid_list_of_strings_param()
        {
            var urlList = new List<string>();
            urlList.Add("seed1.network.atlascity.io");
            urlList.Add("seed2.network.atlascity.io");

            var getTxtRecords = Substitute.For<IDns>().GetTxtRecords(urlList);
            getTxtRecords.Should()
               .BeOfType<Task<IList<IDnsQueryResponse>>>();
            
        }
    }
}