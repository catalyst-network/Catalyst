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

using Catalyst.Cli.Commands;
using Catalyst.Cli.Tests.UnitTests.Helpers;
using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.UnitTests.Commands.Request
{
    public sealed class GetPeerReputationRequestTests
    {
        [Test]
        public void GetPeerReputation_Can_Be_Sent()
        {
            //Arrange
            var commandContext = TestCommandHelpers.GenerateCliRequestCommandContext();
            var connectedNode = commandContext.GetConnectedNode(null);
            var command = new PeerReputationCommand(commandContext, Substitute.For<ILogger>());
            var address = "/ip4/127.0.0.1/tcp/42066/ipfs/18n3naE9kBZoVvgYMV6saMZdwu2yu3QMzKa2BDkb5C5pcuhtrH1G9HHbztbbxA8tGmf4";

            //Act
            TestCommandHelpers.GenerateRequest(commandContext, command, "-n", "node1", "-a", address);

            //Assert
            var requestSent = TestCommandHelpers.GetRequest<GetPeerReputationRequest>(connectedNode);
            requestSent.Should().BeOfType(typeof(GetPeerReputationRequest));
        }
    }
}
