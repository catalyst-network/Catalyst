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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Autofac;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.UnitTests.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit.Abstractions;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    public abstract class CliCommandTestBase : ConfigFileBasedTest
    {
        protected readonly INodeRpcClient NodeRpcClient;

        public static IEnumerable<object[]> AddFileData =>
            new List<object[]>
            {
                new object[] {"/fake_file_path", false},
                new object[] {AppDomain.CurrentDomain.BaseDirectory + "/Config/addfile_test.json", true}
            };
        
        public static IEnumerable<object[]> GetFileData =>
            new List<object[]>
            {
                new object[] {"/fake_file_hash", AppDomain.CurrentDomain.BaseDirectory + "/Config/addfile_test.json", true}
            };

        public CliCommandTestBase(ITestOutputHelper output) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile))
               .Build();

            var channel = Substitute.For<IChannel>();
            channel.Active.Returns(true);

            NodeRpcClient = Substitute.For<INodeRpcClient>();
            NodeRpcClient.Channel.Returns(channel);
            NodeRpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));

            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            nodeRpcClientFactory
               .GetClient(Arg.Any<X509Certificate2>(), Arg.Is<IRpcNodeConfig>(c => c.NodeId == "node1"))
               .Returns(NodeRpcClient);

            ConfigureContainerBuilder(config);

            ContainerBuilder.RegisterInstance(nodeRpcClientFactory).As<INodeRpcClientFactory>();
        }
    }
}
