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
using Catalyst.Common.UnitTests.TestUtils;
using Xunit;
using Xunit.Abstractions;
using NSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Autofac;
using Autofac.Configuration;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Constants = Catalyst.Common.Config.Constants;

namespace Catalyst.Cli.UnitTests
{
    public sealed class CliCommandsTests : ConfigFileBasedTest
    {
        private readonly INodeRpcClient _nodeRpcClient;

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

        public CliCommandsTests(ITestOutputHelper output) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellConfigFile))
               .Build();

            var channel = Substitute.For<IChannel>();
            channel.Active.Returns(true);

            _nodeRpcClient = Substitute.For<INodeRpcClient>();
            _nodeRpcClient.Channel.Returns(channel);
            _nodeRpcClient.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));

            var nodeRpcClientFactory = Substitute.For<INodeRpcClientFactory>();
            nodeRpcClientFactory
               .GetClient(Arg.Any<X509Certificate>(), Arg.Is<IRpcNodeConfig>(c => c.NodeId == "node1"))
               .Returns(_nodeRpcClient);

            ConfigureContainerBuilder(config);

            ContainerBuilder.RegisterInstance(nodeRpcClientFactory).As<INodeRpcClientFactory>();
        }

        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        [Fact]
        public void Cli_Can_Connect_To_Node()
        {
            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();
            }
        }

        [Fact]
        public void Cli_Can_Handle_Multiple_Connection_Attempts()
        {
            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                for (var i = 0; i < 10; i++)
                {
                    var canConnect = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                    canConnect.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Info()
        {
            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var result = shell.AdvancedShell.ParseCommand("getinfo", "-i", "node1");
                result.Should().BeTrue();
            }
        }

        [Fact]
        public void Cli_Can_Request_Node_Version()
        {
            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var result = shell.AdvancedShell.ParseCommand("getversion", "-v", "node1");
                result.Should().BeTrue();
            }
        }

        [Fact] 
        public void Cli_Can_Request_Node_Mempool()
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();

                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand("getmempool", "-m", "node1");
                result.Should().BeTrue();
            }
        }

        [Fact] 
        public void Cli_Can_Request_Node_To_Sign_A_Message()
        {
            var container = ContainerBuilder.Build();
            
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var result = shell.AdvancedShell.ParseCommand("sign", "-m", "test message", "-n", "node1");
                result.Should().BeTrue();
            }
        }

        [Fact] 
        public void Cli_Can_Verify_Message()
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand(
                    "verify", "-m", "test message", "-k", "public_key", "-s", "signature", "-n", "node1");
                result.Should().BeTrue();
            }
        }
        
        [Fact] 
        public void Cli_Can_Send_List_Peers_Request()
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand(
                    "listpeers", "-n", "node1");
                result.Should().BeTrue();
                _nodeRpcClient.Received(1).SendMessage(Arg.Is<AnySigned>(x => x.TypeUrl.Equals(GetPeerListRequest.Descriptor.ShortenedFullName())));
            }
        }
        
        [Fact] 
        public void Cli_Can_Send_Peers_Count_Request()
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand(
                    "peercount", "-n", "node1");
                result.Should().BeTrue();
                _nodeRpcClient.Received(1).SendMessage(Arg.Is<AnySigned>(x => x.TypeUrl.Equals(GetPeerCountRequest.Descriptor.ShortenedFullName())));
            }
        }
        
        [Fact] 
        public void Cli_Can_Send_Remove_Peer_Request()
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand(
                    "removepeer", "-n", "node1", "-k", "fake_public_key", "-i", "127.0.0.1");
                result.Should().BeTrue();
                _nodeRpcClient.Received(1).SendMessage(Arg.Is<AnySigned>(x => x.TypeUrl.Equals(RemovePeerRequest.Descriptor.ShortenedFullName())));
            }
        }
        
        [Fact] 
        public void Cli_Can_Send_Peer_Reputation_Request()
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand(
                    "peerrep", "-n", "node1", "-l", "127.0.0.1", "-p", "fake_public_key");
                result.Should().BeTrue();
                _nodeRpcClient.Received(1).SendMessage(Arg.Is<AnySigned>(x => x.TypeUrl.Equals(GetPeerReputationRequest.Descriptor.ShortenedFullName())));
            }
        }
        
        [Theory]
        [MemberData(nameof(AddFileData))]
        public void Cli_Can_Send_Add_File_Request(string fileName, bool expectedResult)
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand(
                    "addfile", "-n", "node1", "-f", fileName);
                result.Should().Be(expectedResult);

                if (expectedResult)
                {
                    _nodeRpcClient.Received(1).SendMessage(Arg.Is<AnySigned>(x => x.TypeUrl.Equals(AddFileToDfsRequest.Descriptor.ShortenedFullName())));
                }
            }
        }
        
        [Theory]
        [MemberData(nameof(GetFileData))]
        public void Cli_Can_Send_Get_File_Request(string fileHasg, string outputPath, bool expectedResult)
        {
            var container = ContainerBuilder.Build();

            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var shell = container.Resolve<ICatalystCli>();
                var hasConnected = shell.AdvancedShell.ParseCommand("connect", "-n", "node1");
                hasConnected.Should().BeTrue();

                var node1 = shell.AdvancedShell.GetConnectedNode("node1");
                node1.Should().NotBeNull("we've just connected it");

                var result = shell.AdvancedShell.ParseCommand(
                    "getfile", "-n", "node1", "-f", fileHasg, "-o", outputPath);
                result.Should().Be(expectedResult);

                if (expectedResult)
                {
                    _nodeRpcClient.Received(1).SendMessage(Arg.Is<AnySigned>(x => x.TypeUrl.Equals(GetFileFromDfsRequest.Descriptor.ShortenedFullName())));
                }
            }
        }
    }
}
