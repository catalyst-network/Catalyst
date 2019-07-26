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
using System.Text;
using Catalyst.Cli.Commands;
using Catalyst.Cli.UnitTests.Helpers;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using Multiformats.Hash;
using NSubstitute;
using Xunit;

/* This class needs unit tests to test functionality */
namespace Catalyst.Cli.UnitTests.Commands.Request
{
    public sealed class GetDeltaRequestTests
    {
        /* Commented out as I don't have a valid multihash */
        [Fact]
        public void GetDeltaRequest_Can_Be_Sent()
        {
            //Arrange
            var hashingAlgorithm = Common.Config.Constants.HashAlgorithm;
            var deltaMultiHash = Encoding.UTF8.GetBytes("previous").ComputeMultihash(hashingAlgorithm);
            var commandContext = TestCommandHelpers.GenerateCliRequestCommandContext();
            var connectedNode = commandContext.GetConnectedNode(null);
            var command = new GetDeltaCommand(commandContext);

            //Act
            TestCommandHelpers.GenerateRequest(commandContext, command, "-n", "node1", "-h", deltaMultiHash);

            //Assert
            var requestSent = TestCommandHelpers.GetRequest<GetDeltaRequest>(connectedNode);
            requestSent.Should().BeOfType(typeof(GetDeltaRequest));
        }

        [Fact]
        public void GetDeltaRequest_Should_Be_Invalid_Multihash()
        {
            //Arrange
            var hash = "test";
            var commandContext = TestCommandHelpers.GenerateCliRequestCommandContext();
            var command = new GetDeltaCommand(commandContext);

            //Act
            TestCommandHelpers.GenerateRequest(commandContext, command, "-n", "node1", "-h", hash);

            //Assert
            commandContext.UserOutput.Received(1).WriteLine($"Unable to parse hash {hash} as a Multihash");
        }
    }
}
