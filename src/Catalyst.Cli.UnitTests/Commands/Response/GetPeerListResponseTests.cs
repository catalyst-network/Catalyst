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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;

namespace Catalyst.Cli.UnitTests.Commands.Response
{
    public sealed class GetPeerListResponseTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();

        [Fact]
        public void GetPeerListResponse_Can_Get_Output()
        {
            var getPeerListResponse = new GetPeerListResponse();
            getPeerListResponse.Peers.Add(new PeerId());

            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, getPeerListResponse);

            var getPeerListCommand = new PeerListCommand(commandContext);

            _testScheduler.Start();

            commandContext.UserOutput.Received(1).WriteLine(getPeerListResponse.ToJsonString());
        }
    }
}
