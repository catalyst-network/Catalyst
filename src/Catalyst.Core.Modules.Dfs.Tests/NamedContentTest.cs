using Catalyst.Abstractions.Dfs;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    public class NamedContentTest
    {
        [Fact]
        public void Properties()
        {
            var nc = new NamedContent
            {
                ContentPath = "/ipfs/...",
                NamePath = "/ipns/..."
            };
            Assert.Equal("/ipfs/...", nc.ContentPath);
            Assert.Equal("/ipns/...", nc.NamePath);
        }
    }
}
