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
using Catalyst.Cli.UnitTests.Helpers;
using Catalyst.Protocol;
using Catalyst.Protocol.Rpc.Node;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;

namespace Catalyst.Cli.UnitTests.Commands.Response
{
    /// <summary>
    ///     Tests the CLI for peer reputation response
    /// </summary>
    public sealed class GetPeerReputationResponseTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();

        [Fact]
        public void GetPeerReputationResponse_Can_Get_Output()
        {
            //Arrange
            var getPeerReputationResponse = new GetPeerReputationResponse {Reputation = 100};
            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);
            var getPeerReputationCommand = new PeerReputationCommand(commandContext);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, getPeerReputationResponse);

            _testScheduler.Start();

            //Assert
            commandContext.UserOutput.Received(1).WriteLine(getPeerReputationResponse.ToJsonString());
        }
    }
}
