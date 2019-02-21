using System.IO;
using Autofac;
using Catalyst.Node.Common.Modules;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Dfs
{
    public class DfsModuleTest : BaseModuleConfigTest
    {
        public DfsModuleTest() 
            : base(Path.Combine(Constants.ConfigFolder, Constants.ComponentsJsonConfigFile)) {}

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void ComponentsJsonFile_should_configure_DfsModule()
        {
            var resolved = Container.Resolve<IDfs>();
            resolved.Should().NotBeNull();
            resolved.Should().BeOfType<IpfsDfs>();
        }
    }
}
