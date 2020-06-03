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
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Observers
{
    public sealed class RequestObserverBaseTests
    {
        [Test]
        public void OnNext_Should_Still_Get_Called_After_HandleBroadcast_Failure()
        {
            var testScheduler = new TestScheduler(); 
            var peerClient  = Substitute.For<ILibP2PPeerClient>();
            var candidateDeltaMessages = Enumerable.Repeat(new PeerNeighborsRequest(), 10).ToArray();
            var peerSettings = MultiAddressHelper.GetAddress("server").ToSubstitutedPeerSettings();
            var messageStream = MessageStreamHelper.CreateStreamWithMessages(testScheduler, candidateDeltaMessages);
            using (var observer = new FailingRequestObserver(Substitute.For<ILogger>(),
                peerSettings,
                peerClient))
            {
                observer.StartObserving(messageStream);

                testScheduler.Start();

                observer.Counter.Should().Be(10);
            }
        }
    }
}
