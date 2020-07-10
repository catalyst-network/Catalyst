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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    public sealed class GetVersionRequestObserverTests
    {
        private readonly ILogger _logger;
        private readonly IPeerClient _peerClient;

        public GetVersionRequestObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _peerClient = Substitute.For<IPeerClient>();
        }

        [Test]
        public void Valid_GetVersion_Request_Should_Send_VersionResponse()
        {
            var testScheduler = new TestScheduler();

            var versionRequest = new VersionRequest();
            var protocolMessage = versionRequest.ToProtocolMessage(MultiAddressHelper.GetAddress("sender"));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(testScheduler,
                protocolMessage
            );

            var peerSettings = MultiAddressHelper.GetAddress("sender").ToSubstitutedPeerSettings();
            var handler = new GetVersionRequestObserver(peerSettings, _peerClient, _logger);

            handler.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _peerClient.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponse = (ProtocolMessage) receivedCalls.Single().GetArguments().First();
            var versionResponseMessage = sentResponse.FromProtocolMessage<VersionResponse>();
            versionResponseMessage.Version.Should().Be(NodeUtil.GetVersion());
        }
    }
}
