#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using Autofac;
using Catalyst.Common.Interfaces.Cli;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.TestUtils;
using Catalyst.Core.Lib.IntegrationTests;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Cli.IntegrationTests.Connection
{
    public sealed class CliToNodeTests : ConfigFileBasedTest
    {
        private readonly TestCatalystNode _node;
        protected override IEnumerable<string> ConfigFilesUsed { get; }

        public CliToNodeTests(ITestOutputHelper output) : base(output)
        {
            _node = new TestCatalystNode("server", output);

            ConfigFilesUsed = new[]
            {
                Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile),
                Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile),
                Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile)
            };

            ConfigureContainerBuilder();

            ContainerBuilder.RegisterInstance(FileSystem).As<IFileSystem>();
        }

        [Fact]
        public void CliToNode_Connect_To_Node()
        {
            _node.BuildNode();

            using (var container = ContainerBuilder.Build())
            {
                using (var scope = container.BeginLifetimeScope(CurrentTestName))
                {
                    var shell = scope.Resolve<ICatalystCli>();
                    var hasConnected = shell.ParseCommand("connect", "-n", "node1");
                    hasConnected.Should().BeTrue();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _node?.Dispose();
        }
    }
}
