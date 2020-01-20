using System.Collections.Generic;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Modules.Sync.Tests
{
    public class SyncUnitTests
    {
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
            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.PeerId.Returns(PeerIdHelper.GetPeerId()); 
            
            _ledger = Substitute.For<ILedger>();
            _peerClient = Substitute.For<IPeerClient>();

            _deltaIndexService = Substitute.For<IDeltaIndexService>();

            _peerRepository = Substitute.For<IPeerRepository>();
            var peers = new List<Peer>()
            {
                new Peer() {PeerId = PeerIdHelper.GetPeerId()}
            };
            _peerRepository.GetActivePeers(Arg.Any<int>()).Returns(peers);

            _deltaHeightResponseObserver = Substitute.For<IPeerClientObservable>();
            _deltaHistoryResponseObserver = Substitute.For<IPeerClientObservable>();
            _mapperProvider = Substitute.For<IMapperProvider>();
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

        //todo
        [Fact]
        public void Sync_Can_Request_DeltaIndexRange_From_Peer() { }

        //todo
        [Fact]
        public void Sync_Can_Add_DeltaIndexRange_To_Memory() { }

        //todo
        [Fact]
        public void Sync_Can_Update_CurrentDeltaIndex_From_Requested_DeltaIndexRange() { }

        //todo
        [Fact]
        public void Sync_Can_Complete() { }
    }
}
