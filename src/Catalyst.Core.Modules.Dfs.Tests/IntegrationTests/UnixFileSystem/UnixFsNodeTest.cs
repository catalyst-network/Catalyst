using Catalyst.Core.Modules.Dfs.UnixFileSystem;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.UnixFileSystem
{
    public class UnixFsNodeTest
    {
        [Fact]
        public void ToLink()
        {
            var node = new UnixFsNode
            {
                Name = "bar",
                Id = "Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD",
                IsDirectory = true,
                Size = 10,
                DagSize = 16
            };
            var link = node.ToLink("foo");
            Assert.Equal(node.Id, link.Id);
            Assert.Equal(node.DagSize, link.Size);
            Assert.Equal("foo", link.Name);

            link = node.ToLink();
            Assert.Equal(node.Id, link.Id);
            Assert.Equal(node.DagSize, link.Size);
            Assert.Equal("bar", link.Name);
        }
    }
}
