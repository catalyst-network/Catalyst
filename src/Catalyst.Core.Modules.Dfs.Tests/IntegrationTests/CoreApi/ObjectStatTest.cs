using Catalyst.Abstractions.Dfs.CoreApi;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class ObjectStatTest
    {
        [Fact]
        public void Properties()
        {
            var stat = new ObjectStat
            {
                BlockSize = 1,
                CumulativeSize = 2,
                DataSize = 3,
                LinkCount = 4,
                LinkSize = 5
            };
            Assert.Equal(1, stat.BlockSize);
            Assert.Equal(2, stat.CumulativeSize);
            Assert.Equal(3, stat.DataSize);
            Assert.Equal(4, stat.LinkCount);
            Assert.Equal(5, stat.LinkSize);
        }
    }
}
