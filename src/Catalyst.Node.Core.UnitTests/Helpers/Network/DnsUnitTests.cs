using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient.Protocol;
using FluentAssertions;
using Xunit;
using Dns = Catalyst.Node.Core.Helpers.Network.Dns;

namespace Catalyst.Node.Core.UnitTest.Helpers.Network
{
    public class DnsUnitTests
    {
        [Fact]
        public async Task MethodTest()
        {
            Dns dns = new Dns(new IPEndPoint(IPAddress.Parse("9.9.9.9"), 53));
            var dnsQueryResponse =
                await dns.GetTxtRecords("seed1.network.atlascity.io"); //@TODO test the list override method
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            seedIp.Should().Be("92.207.178.198:42069");
        }
    }
}