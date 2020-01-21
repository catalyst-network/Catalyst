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

using System.Collections.Generic;
using System.Reactive.Subjects;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using MultiFormats.Registry;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Modules.Sync.Tests
{
    public class SyncUnitTests
    {
        private readonly IHashProvider _hashProvider;

        private readonly IPeerSettings _peerSettings;
        private readonly ILedger _ledger;
        private readonly IPeerClient _peerClient;
        private IDeltaIndexService _deltaIndexService;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerClientObservable _deltaHeightResponseObserver;
        private readonly IPeerClientObservable _deltaHistoryResponseObserver;
        private readonly IMapperProvider _mapperProvider;

        public SyncUnitTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.PeerId.Returns(PeerIdHelper.GetPeerId());

            _ledger = Substitute.For<ILedger>();
            _peerClient = Substitute.For<IPeerClient>();

            _deltaIndexService = Substitute.For<IDeltaIndexService>();

            _peerRepository = Substitute.For<IPeerRepository>();
            var peers = new List<Peer>
            {
                new Peer {PeerId = PeerIdHelper.GetPeerId()}
            };
            _peerRepository.GetActivePeers(Arg.Any<int>()).Returns(peers);

            _deltaHeightResponseObserver = Substitute.For<IPeerClientObservable>();
            _deltaHistoryResponseObserver = Substitute.For<IPeerClientObservable>();

            _mapperProvider = new TestMapperProvider();
        }

        [Fact]
        public void Can_Restore_DeltaIndex()
        {
            const int sampleCurrentDeltaHeight = 100;
            _deltaIndexService = Substitute.For<IDeltaIndexService>();
            _deltaIndexService.Height().Returns(sampleCurrentDeltaHeight);

            var sync = new Sync(_peerSettings, _ledger, _peerClient, _deltaIndexService, _peerRepository,
                _deltaHeightResponseObserver, _deltaHistoryResponseObserver, _mapperProvider);

            sync.Start();

            sync.CurrentDeltaIndex.Should().Be(sampleCurrentDeltaHeight);
        }

        private DeltaHistoryResponse GenerateSampleData(int count)
        {
            var deltaHeightResponse = new DeltaHistoryResponse();
            var deltaIndexList = new List<DeltaIndex>();
            for (var i = 0; i < count; i++)
            {
                deltaIndexList.Add(new DeltaIndex
                {
                    Cid = ByteString.CopyFrom(_hashProvider.ComputeUtf8MultiHash(i.ToString()).ToCid().ToArray()),
                    Height = (uint) i
                });
            }

            deltaHeightResponse.Result.Add(deltaIndexList);
            return deltaHeightResponse;
        }

        //todo
        [Fact]
        public void Sync_Can_Request_DeltaIndexRange_From_Peer()
        {
            var replaySubject = new ReplaySubject<IPeerClientMessageDto>(1);
            _deltaHistoryResponseObserver.MessageStream.Returns(replaySubject);

            _peerClient.When(x => x.SendMessage(Arg.Any<MessageDto>())).Do(x =>
            {
                var messageDto = (MessageDto) x[0];
                var deltaHeightResponse = GenerateSampleData(10);
                var peerClientMessageDto = new PeerClientMessageDto(deltaHeightResponse, messageDto.SenderPeerIdentifier,
                    CorrelationId.GenerateCorrelationId());
                replaySubject.OnNext(peerClientMessageDto);
            });

            var sync = new Sync(_peerSettings, _ledger, _peerClient, _deltaIndexService, _peerRepository,
                _deltaHeightResponseObserver, _deltaHistoryResponseObserver, _mapperProvider);

            sync.Start();
        }

        //todo
        [Fact]
        public void Sync_Can_Add_DeltaIndexRange_To_Memory() { }

        //todo
        [Fact]
        public void Sync_Can_Update_CurrentDeltaIndex_From_Requested_DeltaIndexRange() { }

        //todo
        [Fact]
        public void Sync_Can_Update_LatestDeltaHeight_From_Requested_DeltaIndexRange() { }

        //todo
        [Fact]
        public void Sync_Can_Complete() { }
    }
}
