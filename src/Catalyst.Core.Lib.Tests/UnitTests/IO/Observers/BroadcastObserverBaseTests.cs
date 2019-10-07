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

using System;
using System.Linq;
using System.Threading;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Ipfs.Registry;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Observers
{
    public class BroadcastObserverBaseTests
    {
        private sealed class FailingBroadCastObserver : BroadcastObserverBase<CandidateDeltaBroadcast>
        {
            private int _counter;
            public int Counter => _counter;

            public FailingBroadCastObserver(ILogger logger) : base(logger) { }

            public override void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto)
            {
                var count = Interlocked.Increment(ref _counter);
                if (count % 2 == 0)
                {
                    throw new ArgumentException("something went wrong handling the request");
                }
            }
        }

        private readonly IHashProvider _hashProvider;

        public BroadcastObserverBaseTests()
        {
            var hashingAlgorithm = HashingAlgorithm.GetAlgorithmMetadata("blake2b-256");
            _hashProvider = new HashProvider(hashingAlgorithm);
        }

        [Fact]
        public void OnNext_Should_Still_Get_Called_After_HandleBroadcast_Failure()
        {
            var testScheduler = new TestScheduler();
            var candidateDeltaMessages = Enumerable.Repeat(DeltaHelper.GetCandidateDelta(_hashProvider), 10).ToArray();

            var messageStream = MessageStreamHelper.CreateStreamWithMessages(testScheduler, candidateDeltaMessages);
            using (var observer = new FailingBroadCastObserver(Substitute.For<ILogger>()))
            {
                observer.StartObserving(messageStream);

                testScheduler.Start();

                observer.Counter.Should().Be(10);
            }
        }
    }
}
