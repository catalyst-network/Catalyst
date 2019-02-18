using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Core.Helpers.Network;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Helpers.Network
{
    public class IpTests
    {
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task GetPublicIp_should_return_a_valid_ip()
        {
            var myIp = await Ip.GetPublicIpAsync();
            myIp.Should().NotBe(default(IPAddress));
        }
    }
}