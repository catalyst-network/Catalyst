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
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.P2P.ReputationSystem;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.ReputationSystem;
using Catalyst.Core.Lib.P2P.Service;
using Catalyst.TestUtils;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.ReputationSystem
{
    public sealed class ReputationManagerTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILogger _subbedLogger;
        private readonly IPeerService _subbedPeerRepository;

        public ReputationManagerTests()
        {
            _testScheduler = new TestScheduler();
            _subbedLogger = Substitute.For<ILogger>();
            _subbedPeerRepository = Substitute.For<IPeerService>();
        }

        [Fact]
        public void Receiving_IPeerReputationChange_Can_Find_And_Update_Peer()
        {
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            var pid = PeerIdHelper.GetPeerId("some_peer");
            peerReputationChange.PeerId.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(100);
            
            var results = new List<Peer>();
            var subbedPeer = new Peer
            {
                PeerId = pid
            };
            results.Add(subbedPeer);
            SetRepoReturnValue(results);

            var reputationManager = new ReputationManager(_subbedPeerRepository, _subbedLogger);
            
            reputationManager.OnNext(peerReputationChange);

            _testScheduler.Start();

            _subbedPeerRepository.ReceivedWithAnyArgs(1).GetAll();
            _subbedPeerRepository.ReceivedWithAnyArgs(1).Update(Arg.Is(subbedPeer));
        }
        
        [Fact]
        public void Receiving_IPeerReputationChange_Can_Increase_Rep()
        {
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            var pid = PeerIdHelper.GetPeerId("some_peer");
            peerReputationChange.PeerId.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(100);
            
            var results = new List<Peer>();
            var subbedPeer = new Peer
            {
                PeerId = pid,
                Reputation = 100
            };
            results.Add(subbedPeer);
            SetRepoReturnValue(results);

            var reputationManager = new ReputationManager(_subbedPeerRepository, _subbedLogger);
            
            reputationManager.OnNext(peerReputationChange);

            _testScheduler.Start();

            _subbedPeerRepository.ReceivedWithAnyArgs(1).GetAll();
            _subbedPeerRepository.ReceivedWithAnyArgs(1).Update(Arg.Is<Peer>(r => r.Reputation == 200));
        }
        
        [Fact]
        public void Receiving_IPeerReputationChange_Can_Decrease_Rep()
        {
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            var pid = PeerIdHelper.GetPeerId("some_peer");
            peerReputationChange.PeerId.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(-100);
            
            var results = new List<Peer>();
            var subbedPeer = new Peer
            {
                PeerId = pid,
                Reputation = 100
            };
            results.Add(subbedPeer);

            SetRepoReturnValue(results);

            var reputationManager = new ReputationManager(_subbedPeerRepository, _subbedLogger);
            
            reputationManager.OnNext(peerReputationChange);

            _testScheduler.Start();

            _subbedPeerRepository.ReceivedWithAnyArgs(1).GetAll();
            _subbedPeerRepository.ReceivedWithAnyArgs(1).Update(Arg.Is<Peer>(r => r.Reputation == 0));
        }

        [Fact]
        public void Can_Merge_Streams_And_Read_Items_Pushed_On_Separate_Streams()
        {
            var pid1 = PeerIdHelper.GetPeerId("peer1");
            var pid2 = PeerIdHelper.GetPeerId("peer2");
            
            var results = new List<Peer>();
            
            var subbedPeer1 = new Peer
            {
                PeerId = pid1,
                Reputation = 100
            };
            
            var peerReputationChangeEvent1 = Substitute.For<IPeerReputationChange>();
            peerReputationChangeEvent1.PeerId.Returns(pid1);
            
            var subbedPeer2 = new Peer
            {
                PeerId = pid2,
                Reputation = 200
            };
            
            var peerReputationChangeEvent2 = Substitute.For<IPeerReputationChange>();
            peerReputationChangeEvent2.PeerId.Returns(pid2);
            
            results.Add(subbedPeer1);
            results.Add(subbedPeer2);
            
            SetRepoReturnValue(results);

            var reputationManager = new ReputationManager(_subbedPeerRepository, _subbedLogger);
        
            var secondStreamSubject = new ReplaySubject<IPeerReputationChange>(1, _testScheduler);
            var messageStream = secondStreamSubject.AsObservable();

            messageStream.Subscribe(reputationChange => Substitute.For<ILogger>());
            
            reputationManager.MergeReputationStream(messageStream);

            var streamObserver = Substitute.For<IObserver<IPeerReputationChange>>();
            
            using (reputationManager.MergedEventStream.Subscribe(streamObserver.OnNext))
            {
                reputationManager.ReputationEvent.OnNext(peerReputationChangeEvent1);
                secondStreamSubject.OnNext(peerReputationChangeEvent2);

                _testScheduler.Start();

                streamObserver.Received(1).OnNext(Arg.Is<IPeerReputationChange>(r => r.PeerId.Equals(pid1)));
                streamObserver.Received(1).OnNext(Arg.Is<IPeerReputationChange>(r => r.PeerId.Equals(pid2)));
            }
        }
        
        private void SetRepoReturnValue(IEnumerable<Peer> list)
        {
            _subbedPeerRepository.GetAll().Returns(list);
        }
    }
}
