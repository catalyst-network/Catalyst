using Catalyst.Abstractions.Options;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    [TestClass]
    public class DiscoveryOptionsTest
    {
        [Fact]
        public void Defaults()
        {
            var options = new DiscoveryOptions();
            Assert.Null(options.BootstrapPeers);
            Assert.False(options.DisableMdns);
        }
    }
}
