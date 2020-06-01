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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Sync.Manager;
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
using Catalyst.Core.Lib.P2P.Repository;
using NUnit.Framework;
using MultiFormats;
using Lib.P2P.Protocols;
using LibP2P = Lib.P2P;
using Catalyst.Abstractions.Dfs.CoreApi;
using System.Linq;
using System;

namespace Catalyst.Core.Modules.Sync.Tests.UnitTests
{
    public class DeltaHeightWatcherUnitTests
    {
        private ILibP2PPeerClient _peerClient;
        private IHashProvider _hashProvider;
        private ILibP2PPeerService _peerService;
        private ISwarmApi _swarmApi;
        private ReplaySubject<IObserverDto<ProtocolMessage>> _deltaHeightReplaySubject;

        [SetUp]
        public void Init()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _peerService = Substitute.For<ILibP2PPeerService>();
            _peerClient = Substitute.For<ILibP2PPeerClient>();
            _swarmApi = Substitute.For<ISwarmApi>();
            _deltaHeightReplaySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1);
            _peerService.MessageStream.Returns(_deltaHeightReplaySubject.AsObservable());
        }

        private void GeneratePeers(int count)
        {
            var peerList = new List<LibP2P.Peer>();

            for (var i = 0; i < count; i++)
            {
                var address = PeerIdHelper.GetPeerId(i.ToString(), port: i);
                var peer = new LibP2P.Peer
                {
                    Id = address.PeerId,
                    ConnectedAddress = address
                };
                peerList.Add(peer);
            }

            _swarmApi.PeersAsync(default).Returns(peerList);
        }

        [Test]
        public async Task GetHighestDeltaIndexAsync_Should_Return_DeltaIndex()
        {
            var deltaHeight = 100u;
            GeneratePeers(100);

            _peerClient.When(x => x.SendMessageToPeersAsync(Arg.Any<IMessage>(), Arg.Any<IEnumerable<MultiAddress>>())).Do(x =>
              {
                  var peerIds = (IEnumerable<MultiAddress>) x[1];
                  foreach (var peerId in peerIds)
                  {
                      var deltaHeightResponse = new LatestDeltaHashResponse
                      {
                          DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(deltaHeight.ToString()).ToCid().ToArray().ToByteString(), Height = deltaHeight },
                          IsSync = true
                      };

                      _deltaHeightReplaySubject.OnNext(new ObserverDto(null, deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                  }
              });

            var deltaHeightWatcher = new DeltaHeightWatcher(_peerClient, _swarmApi, _peerService);
            deltaHeightWatcher.Start();

            var deltaIndex = await deltaHeightWatcher.GetHighestDeltaIndexAsync(TimeSpan.FromSeconds(10));

            deltaIndex.Height.Should().Be(deltaHeight);
        }

        [Test]
        public async Task GetHighestDeltaIndexAsync_Should_Return_Highest_Syncd_DeltaIndex()
        {
            var deltaHeight = 100u;
            GeneratePeers(100);

            _peerClient.When(x => x.SendMessageToPeersAsync(Arg.Any<IMessage>(), Arg.Any<IEnumerable<MultiAddress>>())).Do(x =>
            {
                var peerIds = (IEnumerable<MultiAddress>) x[1];
                foreach (var peerId in peerIds)
                {
                    if (peerId.GetPort() > 50)
                    {
                        var deltaHeightResponse = new LatestDeltaHashResponse
                        {
                            DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(deltaHeight.ToString()).ToCid().ToArray().ToByteString(), Height = deltaHeight },
                            IsSync = true
                        };

                        _deltaHeightReplaySubject.OnNext(new ObserverDto(null, deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                    }
                    else
                    {
                        var badHeight = 999u;
                        var deltaHeightResponse = new LatestDeltaHashResponse
                        {
                            DeltaIndex = new DeltaIndex { Cid = _hashProvider.ComputeUtf8MultiHash(badHeight.ToString()).ToCid().ToArray().ToByteString(), Height = badHeight },
                            IsSync = false
                        };

                        _deltaHeightReplaySubject.OnNext(new ObserverDto(null, deltaHeightResponse.ToProtocolMessage(peerId, CorrelationId.GenerateCorrelationId())));
                    }

                }
            });

            var deltaHeightWatcher = new DeltaHeightWatcher(_peerClient, _swarmApi, _peerService);
            deltaHeightWatcher.Start();

            var deltaIndex = await deltaHeightWatcher.GetHighestDeltaIndexAsync(TimeSpan.FromSeconds(10));

            deltaIndex.Height.Should().Be(deltaHeight);
        }
    }
}
