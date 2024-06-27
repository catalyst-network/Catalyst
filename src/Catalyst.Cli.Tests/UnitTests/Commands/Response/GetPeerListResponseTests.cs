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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.TestUtils;

namespace Catalyst.Cli.Tests.UnitTests.Commands.Response
{
    public sealed class GetPeerListResponseTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();

        [Test]
        public void GetPeerListResponse_Can_Get_Output()
        {
            var getPeerListResponse = new GetPeerListResponse();
            getPeerListResponse.Peers.Add(MultiAddressHelper.GetAddress().ToString());

            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);

            new PeerListCommand(commandContext, Substitute.For<ILogger>());

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, getPeerListResponse);

            _testScheduler.Start();

            commandContext.UserOutput.Received(1).WriteLine(getPeerListResponse.ToJsonString());
        }
    }
}
