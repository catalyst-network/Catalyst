#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.UnitTests.Commands.Request
{
    [TestFixture]
    public sealed class GetDeltaRequestTests
    {
        private readonly ILogger _logger;

        public GetDeltaRequestTests() { _logger = Substitute.For<ILogger>(); }

        [Test]
        public void GetDeltaRequest_Can_Be_Sent()
        {
            //Arrange
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            var deltaMultiHash = hashProvider.ComputeUtf8MultiHash("previous").ToCid();
            var commandContext = TestCommandHelpers.GenerateCliRequestCommandContext();
            var connectedNode = commandContext.GetConnectedNode(null);
            var command = new GetDeltaCommand(commandContext, _logger);

            //Act
            TestCommandHelpers.GenerateRequest(commandContext, command, "-n", "node1", "-h", deltaMultiHash);

            //Assert
            var requestSent = TestCommandHelpers.GetRequest<GetDeltaRequest>(connectedNode);
            requestSent.Should().BeOfType(typeof(GetDeltaRequest));
        }

        [Test]
        public void GetDeltaRequest_Should_Be_Invalid_Multihash()
        {
            //Arrange
            var hash = "test";
            var commandContext = TestCommandHelpers.GenerateCliRequestCommandContext();
            var command = new GetDeltaCommand(commandContext, _logger);

            //Act
            TestCommandHelpers.GenerateRequest(commandContext, command, "-n", "node1", "-h", hash);

            //Assert
            commandContext.UserOutput.Received(1).WriteLine($"Unable to parse hash {hash} as a Cid");
        }
    }
}
