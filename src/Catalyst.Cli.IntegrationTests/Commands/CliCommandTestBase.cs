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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit.Abstractions;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    public abstract class CliCommandTestBase : ConfigFileBasedTest
    {
        protected INodeRpcClient NodeRpcClient;

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

        protected CliCommandTestBase(ITestOutputHelper output, bool bSubstituteNodeClient = true) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile))
               .Build();

            ConfigureContainerBuilder(config);

            if (bSubstituteNodeClient) ConfigureNodeClient();
        }

        protected void ConfigureNodeClient()
        {
            var channel = Substitute.For<IChannel>();
            channel.Active.Returns(true);

            NodeRpcClient = Substitute.For<INodeRpcClient>();
            NodeRpcClient.Channel.Returns(channel);
            NodeRpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));

            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            nodeRpcClientFactory
               .GetClient(Arg.Any<X509Certificate2>(), Arg.Is<IRpcNodeConfig>(c => c.NodeId == "node1"))
               .Returns(NodeRpcClient);

            ContainerBuilder.RegisterInstance(nodeRpcClientFactory).As<INodeRpcClientFactory>();
        }

        protected void AssertSentMessage<T>() where T : IMessage<T>
        {
            NodeRpcClient.Received(1).SendMessage(Arg.Is<IMessageDto<ProtocolMessage>>(x =>
                x.Content != null &&
                x.Content.GetType().IsAssignableTo<ProtocolMessage>() &&
                x.Content.FromProtocolMessage<T>() != null
            ));
        }
    }
}
