using Catalyst.Abstractions.Options;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    public class SwarmOptionsTest
    {
        [Fact]
        public void Defaults()
        {
            var options = new SwarmOptions();
            Assert.Null(options.PrivateNetworkKey);
            Assert.Equal(8, options.MinConnections);
        }
    }
}
