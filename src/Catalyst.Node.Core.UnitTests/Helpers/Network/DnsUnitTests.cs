using System.Linq;
using Catalyst.Node.Core.Helpers.Network;
using DnsClient.Protocol;
using Xunit;
using FluentAssertions;

namespace Catalyst.Node.UnitTests.Helpers.Network
{
    public class DnsUnitTests
    {

        [Fact]
        public void MethodTest()
        {
            var dnsQueryResponse = Dns.GetTxtRecords("seed1.network.atlascity.io"); //@TODO test the list override method
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            seedIp.Should().Be("92.207.178.198:42069");
        }
    }
}