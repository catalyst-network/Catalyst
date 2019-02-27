using System.Collections.Generic;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Modules;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Modules
{
    public static class ModuleNamesTests
    {
        [Fact]
        public static void All_should_return_all_declared_names()
        {
            var allModuleNames = Enumeration.GetAll<ModuleName>();
             
            var expectedList = new List<string>
                { "Consensus", "Contract", "Dfs", "Gossip", "Ledger", "Mempool" };

            allModuleNames.Should().BeEquivalentTo(expectedList);
        }
    }
}
