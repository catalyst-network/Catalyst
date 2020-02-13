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

using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Sync.Watcher;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using MultiFormats.Registry;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Catalyst.Core.Modules.Sync.Tests.UnitTests
{
    public class DeltaHeightWatcherUnitTests
    {
        private IHashProvider _hashProvider;
        private IPeerSyncManager _peerSyncManager;
        private IPeerService _peerService;
        private IPeerRepository _peerRepository;
        private ReplaySubject<IObserverDto<ProtocolMessage>> _deltaHeightReplaySubject;

        public DeltaHeightWatcherUnitTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _peerSyncManager = Substitute.For<IPeerSyncManager>();
            _peerService = Substitute.For<IPeerService>();
            _peerRepository = new PeerRepository(new InMemoryRepository<Peer, string>());
            _deltaHeightReplaySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1);
            _peerService.MessageStream.Returns(_deltaHeightReplaySubject.AsObservable());
        }

        private void GeneratePeers(int count)
        {
            for (var i = 0; i < 100; i++)
            {
                var peer = new Peer
                {
                    PeerId = PeerIdHelper.GetPeerId(port: i)
                };
                _peerRepository.Add(peer);
            }
        }

        [Fact]
        public async Task GetHighestDeltaIndexAsync_Should_Return_DeltaIndex()
        {
            var deltaHeight = 100u;
            GeneratePeers(100);

            _peerSyncManager.When(x => x.SendMessageToPeers(Arg.Any<IMessage>(), Arg.Any<IEnumerable<PeerId>>())).Do(x =>
              {
                  var peerIds = (IEnumerable<PeerId>)x[1];
                  foreach (var peerId in peerIds)
                  {
                      var deltaHeightResponse = new LatestDeltaHashResponse
                      {
                          DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(deltaHeight.ToString()).ToCid().ToArray().ToByteString(), Height = deltaHeight },
                          IsSync = true
                      };

                      _deltaHeightReplaySubject.OnNext(new ObserverDto(Substitute.For<IChannelHandlerContext>(),
                           deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                  }
              });

            var deltaHeightWatcher = new DeltaHeightWatcher(_peerSyncManager, _peerRepository, _peerService);
            deltaHeightWatcher.Start();

            var deltaIndex = await deltaHeightWatcher.GetHighestDeltaIndexAsync();

            deltaIndex.Height.Should().Be(deltaHeight);
        }

        [Fact]
        public async Task GetHighestDeltaIndexAsync_Should_Return_Highest_Syncd_DeltaIndex()
        {
            var deltaHeight = 100u;
            GeneratePeers(100);

            _peerSyncManager.When(x => x.SendMessageToPeers(Arg.Any<IMessage>(), Arg.Any<IEnumerable<PeerId>>())).Do(x =>
            {
                var peerIds = (IEnumerable<PeerId>)x[1];
                foreach (var peerId in peerIds)
                {
                    if (peerId.Port > 50)
                    {
                        var deltaHeightResponse = new LatestDeltaHashResponse
                        {
                            DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(deltaHeight.ToString()).ToCid().ToArray().ToByteString(), Height = deltaHeight },
                            IsSync = true
                        };

                        _deltaHeightReplaySubject.OnNext(new ObserverDto(Substitute.For<IChannelHandlerContext>(),
     deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                    }
                    else
                    {
                        var badHeight = 999u;
                        var deltaHeightResponse = new LatestDeltaHashResponse
                        {
                            DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(badHeight.ToString()).ToCid().ToArray().ToByteString(), Height = badHeight },
                            IsSync = false
                        };

                        _deltaHeightReplaySubject.OnNext(new ObserverDto(Substitute.For<IChannelHandlerContext>(),
     deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                    }

                }
            });

            var deltaHeightWatcher = new DeltaHeightWatcher(_peerSyncManager, _peerRepository, _peerService);
            deltaHeightWatcher.Start();

            var deltaIndex = await deltaHeightWatcher.GetHighestDeltaIndexAsync();

            deltaIndex.Height.Should().Be(deltaHeight);
        }

        [Fact]
        public async Task GetHighestDeltaIndexAsync_Should_Return_Highest_UnSyncd_DeltaIndex()
        {
            GeneratePeers(100);

            _peerSyncManager.When(x => x.SendMessageToPeers(Arg.Any<IMessage>(), Arg.Any<IEnumerable<PeerId>>())).Do(x =>
            {
                var peerIds = (IEnumerable<PeerId>)x[1];
                foreach (var peerId in peerIds)
                {
                    var height = peerId.Port;
                    var deltaHeightResponse = new LatestDeltaHashResponse
                    {
                        DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(height.ToString()).ToCid().ToArray().ToByteString(), Height = height },
                        IsSync = false
                    };

                    _deltaHeightReplaySubject.OnNext(new ObserverDto(Substitute.For<IChannelHandlerContext>(),
 deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                }
            });

            var deltaHeightWatcher = new DeltaHeightWatcher(_peerSyncManager, _peerRepository, _peerService);
            deltaHeightWatcher.Start();

            var deltaIndex = await deltaHeightWatcher.GetHighestDeltaIndexAsync();

            deltaIndex.Height.Should().Be((uint) (_peerRepository.Count()-1));
        }

        [Fact]
        public async Task GetHighestDeltaIndexAsync_Should_Return_OnlyPeer_DeltaIndex()
        {
            GeneratePeers(1);

            _peerSyncManager.When(x => x.SendMessageToPeers(Arg.Any<IMessage>(), Arg.Any<IEnumerable<PeerId>>())).Do(x =>
            {
                var peerIds = (IEnumerable<PeerId>)x[1];
                foreach (var peerId in peerIds)
                {
                    var height = peerId.Port;
                    var deltaHeightResponse = new LatestDeltaHashResponse
                    {
                        DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(height.ToString()).ToCid().ToArray().ToByteString(), Height = height },
                        IsSync = false
                    };

                    _deltaHeightReplaySubject.OnNext(new ObserverDto(Substitute.For<IChannelHandlerContext>(),
 deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                }
            });

            var deltaHeightWatcher = new DeltaHeightWatcher(_peerSyncManager, _peerRepository, _peerService);
            deltaHeightWatcher.Start();

            var deltaIndex = await deltaHeightWatcher.GetHighestDeltaIndexAsync();

            deltaIndex.Height.Should().Be((uint)(_peerRepository.Count() - 1));
        }
    }
}
