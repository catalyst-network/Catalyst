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

using Autofac;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    public sealed class PeerListCommandTest : CliCommandTestBase
    {
        //This test is the base to all other tests.  If the Cli cannot connect to a node than all other commands
        //will fail
        public PeerListCommandTest(ITestOutputHelper output) : base(output) { }
        
        [Fact] 
        public void Cli_Can_Send_List_Peers_Request()
        {
            using (var container = ContainerBuilder.Build())
            {
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
                    NodeRpcClient.Received(1).SendMessage(Arg.Is<IMessageDto<GetPeerListRequest>>(
                        x => x.Message != null && 
                            x.Message.GetType().IsAssignableTo<GetPeerListRequest>()));
                }   
            }
        }
    }
}
