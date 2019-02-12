using System.Linq;
using Catalyst.Node.Core.Helpers.Network;
using DnsClient.Protocol;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Helpers.Network
{
    public class DnsUnitTests
    {
        [Fact]
        public void MethodTest()
        {
            Dns dns = new Dns(EndpointBuilder.BuildNewEndPoint("9.9.9.9:53"));
            var dnsQueryResponse =
                dns.GetTxtRecords("seed1.network.atlascity.io"); //@TODO test the list override method
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            seedIp.Should().Be("92.207.178.198:42069");
        }
    }
}