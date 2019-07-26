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

using System.Linq;
using Catalyst.Cli.Commands;
using Catalyst.Cli.UnitTests.Helpers;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Cli.UnitTests.Commands.Request
{
    public sealed class GetPeerReputationRequestTests
    {
        [Fact]
        public void GetPeerReputation_Can_Be_Sent()
        {
            //Arrange
            var commandContext = TestCommandHelpers.GenerateCliRequestCommandContext();
            var connectedNode = commandContext.GetConnectedNode(null);
            var command = new PeerReputationCommand(commandContext);

            //Act
            TestCommandHelpers.GenerateRequest(commandContext, command, "-n", "node1", "-i", "10.1.1.1", "-p", "public key");

            //Assert
            var requestSent = TestCommandHelpers.GetRequest<GetPeerReputationRequest>(connectedNode);
            requestSent.Should().BeOfType(typeof(GetPeerReputationRequest));
        }
    }
}
