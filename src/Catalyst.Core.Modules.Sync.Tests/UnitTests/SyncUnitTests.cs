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
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Sync.Manager;
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Core.Modules.Sync.Watcher;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Lib.P2P;
using Microsoft.Reactive.Testing;
using MultiFormats.Registry;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using Xunit;
using Serilog;
using Peer = Catalyst.Core.Lib.P2P.Models.Peer;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.P2P.Repository;

namespace Catalyst.Core.Modules.Sync.Tests.UnitTests
{
    public class SyncUnitTests
    {
        private IMessenger _messenger;
        private readonly TestScheduler _testScheduler;
        private readonly IHashProvider _hashProvider;
        private readonly IPeerSettings _peerSettings;
        private readonly IDeltaDfsReader _deltaDfsReader;
        private readonly ILedger _ledger;
        private readonly IPeerClient _peerClient;
        private IDeltaIndexService _deltaIndexService;
        private readonly IPeerRepository _peerRepository;

        private readonly IPeerService _peerService;

        private readonly IP2PMessageObserver _deltaHeightResponseObserver;
        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _deltaHeightReplaySubject;

        private readonly IP2PMessageObserver _deltaHistoryResponseObserver;
        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _deltaHistoryReplaySubject;

        private readonly IMapperProvider _mapperProvider;
        private readonly IUserOutput _userOutput;

        private readonly IDeltaCache _deltaCache;

        private readonly IPeerSyncManager _peerSyncManager;
        private IDeltaHeightWatcher _deltaHeightWatcher;

        private readonly IDeltaHashProvider _deltaHashProvider;

        private int _syncTestHeight = 1005;

        private readonly ManualResetEventSlim _manualResetEventSlim;

        private readonly CancellationToken _cancellationToken;

        public SyncUnitTests()
        {
            _cancellationToken = new CancellationToken();

            _manualResetEventSlim = new ManualResetEventSlim(false);

            _testScheduler = new TestScheduler();
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.PeerId.Returns(PeerIdHelper.GetPeerId());

            _deltaDfsReader = Substitute.For<IDeltaDfsReader>();
            _deltaDfsReader.TryReadDeltaFromDfs(Arg.Any<Cid>(), out Arg.Any<Delta>()).Returns(x => true);

            _deltaCache = Substitute.For<IDeltaCache>();
            _deltaCache.GenesisHash.Returns("bafk2bzacecji5gcdd6lxsoazgnbg46c3vttjwwkptiw27enachziizhhkir2w".ToCid());

            _ledger = Substitute.For<ILedger>();

            _peerService = Substitute.For<IPeerService>();

            _deltaHashProvider = new DeltaHashProvider(_deltaCache, Substitute.For<ILogger>());

            _deltaIndexService = new DeltaIndexService(new InMemoryRepository<DeltaIndexDao, string>());
            _deltaIndexService.Add(new DeltaIndexDao { Cid = _hashProvider.ComputeUtf8MultiHash("0").ToCid(), Height = 0 });

            _peerClient = Substitute.For<IPeerClient>();
            ModifyPeerClient<LatestDeltaHashRequest>((request, senderPeerIdentifier) =>
            {
                var deltaHeightResponse = new LatestDeltaHashResponse
                {
                    DeltaIndex = new DeltaIndex
                    {
                        Cid = _hashProvider.ComputeUtf8MultiHash(_syncTestHeight.ToString()).ToCid().ToArray()
                           .ToByteString(),
                        Height = (uint)_syncTestHeight
                    }
                };

                //var peerClientMessageDto = new PeerClientMessageDto(deltaHeightResponse,
                //    senderPeerIdentifier,
                //    CorrelationId.GenerateCorrelationId());
                _deltaHeightReplaySubject.OnNext(new ObserverDto(Substitute.For<IChannelHandlerContext>(),
                    deltaHeightResponse.ToProtocolMessage(senderPeerIdentifier, CorrelationId.GenerateCorrelationId())));
            });

            ModifyPeerClient<DeltaHistoryRequest>((request, senderPeerIdentifier) =>
            {
                //var peerClientMessageDto = new PeerClientMessageDto(
                //    GenerateSampleData((int) request.Height, (int) request.Range, _syncTestHeight),
                //    senderPeerIdentifier,
                //    CorrelationId.GenerateCorrelationId());

                var data = GenerateSampleData((int)request.Height, (int)request.Range, _syncTestHeight);
                _deltaIndexService.Add(data.DeltaIndex.Select(x => DeltaIndexDao.ToDao<DeltaIndex>(x, _mapperProvider)));

                _deltaHistoryReplaySubject.OnNext(new ObserverDto(Substitute.For<IChannelHandlerContext>(),
                    data
                       .ToProtocolMessage(senderPeerIdentifier, CorrelationId.GenerateCorrelationId())));
            });

            _peerRepository = new PeerRepository(new InMemoryRepository<Peer, string>());
            Enumerable.Repeat(new Peer { PeerId = PeerIdHelper.GetPeerId() }, 5).ToList().ForEach(_peerRepository.Add);
            //_peerRepository.GetActivePeers(Arg.Any<int>()).Returns(peers);
            //_peerRepository.Count().Returns(5);

            //_deltaHeightResponseObserver = Substitute.For<IP2PMessageObserver>();
            _deltaHeightReplaySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1);

            //_deltaHeightResponseObserver.StartObserving().Returns(_deltaHeightReplaySubject);

            //_deltaHistoryResponseObserver = Substitute.For<IP2PMessageObserver>();
            _deltaHistoryReplaySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1);

            //_deltaHistoryResponseObserver.StartObserving();.Returns(_deltaHistoryReplaySubject);

            var mergeMessageStreams = _deltaHeightReplaySubject.AsObservable()
               .Merge(_deltaHistoryReplaySubject.AsObservable());

            _peerService.MessageStream.Returns(mergeMessageStreams);

            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _deltaHashProvider.TryUpdateLatestHash(Arg.Any<Cid>(), Arg.Any<Cid>()).Returns(true);
            //_deltaHashProvider.When(x => x.TryUpdateLatestHash(Arg.Any<Cid>(), Arg.Any<Cid>())).Do(x =>
            //{
            //    var prevCid = (Cid) x[0];
            //    var newCid = (Cid) x[1];

            //    _deltaIndexService.Add(newCid);
            //});

            _mapperProvider = new TestMapperProvider();

            _userOutput = Substitute.For<IUserOutput>();

            _messenger = new Messenger(_peerClient, _peerSettings);

            _deltaHeightWatcher = new DeltaHeightWatcher(_messenger, _peerRepository, _peerService);

            var dfsService = Substitute.For<IDfsService>();

            _peerSyncManager = new PeerSyncManager(_messenger, _peerRepository,
                _peerService, _userOutput, _deltaHeightWatcher, Substitute.For<IDfsService>(), 0.7, 0);
        }

        private DeltaHistoryResponse GenerateSampleData(int height, int range, int maxHeight = -1)
        {
            var deltaHeightResponse = new DeltaHistoryResponse();
            var deltaIndexList = new List<DeltaIndex>();
            var heightSum = height + range;

            if (heightSum > maxHeight)
            {
                heightSum = maxHeight;
            }

            for (var i = height; i <= heightSum; i++)
            {
                deltaIndexList.Add(new DeltaIndex
                {
                    Cid = ByteString.CopyFrom(_hashProvider.ComputeUtf8MultiHash(i.ToString()).ToCid().ToArray()),
                    Height = (uint)i
                });
            }

            deltaHeightResponse.DeltaIndex.Add(deltaIndexList);
            return deltaHeightResponse;
        }

        private void ModifyPeerClient<TRequest>(Action<TRequest, PeerId> callback) where TRequest : IMessage<TRequest>
        {
            _peerClient.When(x =>
                x.SendMessage(
                    Arg.Is<MessageDto>(y => y.Content.TypeUrl.EndsWith(typeof(TRequest).Name)))).Do(
                z =>
                {
                    var messageDto = (MessageDto)z[0];
                    callback.Invoke(messageDto.Content.FromProtocolMessage<TRequest>(),
                        messageDto.SenderPeerIdentifier);
                });
        }

        [Fact]
        public async Task StartAsync_Should_Start_Sync()
        {
            _deltaHeightWatcher = Substitute.For<IDeltaHeightWatcher>();
            _deltaHeightWatcher.GetHighestDeltaIndexAsync().Returns(new DeltaIndex { Cid = ByteString.Empty, Height = 10000 });

            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader,
                _deltaIndexService, Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StartAsync(CancellationToken.None);

            sync.State.IsRunning.Should().BeTrue();
        }

        [Fact]
        public async Task StopAsync_Should_Stop_Sync()
        {
            _deltaHeightWatcher = Substitute.For<IDeltaHeightWatcher>();
            _deltaHeightWatcher.GetHighestDeltaIndexAsync().Returns(new DeltaIndex { Cid = ByteString.Empty, Height = 10000 });

            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader,
                _deltaIndexService, Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StartAsync(CancellationToken.None);
            sync.State.IsRunning.Should().BeTrue();

            await sync.StopAsync(CancellationToken.None);
            sync.State.IsRunning.Should().BeFalse();
        }

        [Fact]
        public async Task StopAsync_Should_Log_If_Not_Running_Sync()
        {
            _deltaHeightWatcher = Substitute.For<IDeltaHeightWatcher>();
            _deltaHeightWatcher.GetHighestDeltaIndexAsync().Returns(new DeltaIndex { Cid = ByteString.Empty, Height = 10000 });

            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader,
                _deltaIndexService, Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StopAsync(CancellationToken.None);

            _userOutput.Received(1).WriteLine("Sync is not currently running.");

            sync.State.IsRunning.Should().BeFalse();
        }

        [Fact]
        public async Task Can_Restore_DeltaIndex()
        {
            const int sampleCurrentDeltaHeight = 100;

            var cid = _hashProvider.ComputeUtf8MultiHash(sampleCurrentDeltaHeight.ToString()).ToCid();
            _deltaIndexService.Add(new DeltaIndexDao { Cid = cid, Height = sampleCurrentDeltaHeight });

            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, Substitute.For<IDeltaHeightWatcher>(), _deltaHashProvider, _deltaDfsReader,
                _deltaIndexService, Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StartAsync(CancellationToken.None);

            sync.CurrentHighestDeltaIndexStored.Should().Be(sampleCurrentDeltaHeight);
        }

        [Fact]
        public async Task Sync_Can_Add_DeltaIndexRange_To_Repository()
        {
            _syncTestHeight = 10;
            var expectedData = GenerateSampleData(0, _syncTestHeight, _syncTestHeight);
            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader, _deltaIndexService,
                Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>(), _syncTestHeight);

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            var range = _deltaIndexService.GetRange(0, _syncTestHeight).Select(x => DeltaIndexDao.ToProtoBuff<DeltaIndex>(x, _mapperProvider));
            range.Should().BeEquivalentTo(expectedData.DeltaIndex);
        }

        [Fact]
        public async Task Sync_Can_Download_Deltas()
        {
            _syncTestHeight = 10;

            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader, _deltaIndexService,
                Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>(), _syncTestHeight);

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            _deltaCache.Received(_syncTestHeight).TryGetOrAddConfirmedDelta(Arg.Any<Cid>(), out Arg.Any<Delta>());
        }

        [Fact]
        public async Task Sync_Can_Update_State()
        {
            _syncTestHeight = 10;

            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader, _deltaIndexService,
                Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>(), _syncTestHeight);

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            _deltaHashProvider.Received(_syncTestHeight).TryUpdateLatestHash(Arg.Any<Cid>(), Arg.Any<Cid>());
        }

        [Fact]
        public async Task Sync_Can_Update_CurrentDeltaIndex_From_Requested_DeltaIndexRange()
        {
            _syncTestHeight = 10;

            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader, _deltaIndexService,
                Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            sync.CurrentHighestDeltaIndexStored.Should().Be(10);
        }

        [Fact]
        public async Task Sync_Can_Complete()
        {
            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader, _deltaIndexService,
                Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            sync.CurrentHighestDeltaIndexStored.Should().Be(_syncTestHeight);
        }

        private Dictionary<Cid, Delta> BuildChainedDeltas(int chainSize)
        {
            var chainedDeltas = Enumerable.Range(0, chainSize + 1).ToDictionary(
                i => _hashProvider.ComputeUtf8MultiHash(i.ToString()).ToCid(),
                i =>
                {
                    var previousHash = _hashProvider.ComputeUtf8MultiHash((i - 1).ToString()).ToCid();
                    var delta = DeltaHelper.GetDelta(_hashProvider, previousHash);
                    return delta;
                });

            _userOutput.WriteLine("chain is:");
            _userOutput.WriteLine(string.Join(Environment.NewLine,
                chainedDeltas.Select((c, i) =>
                    $"{i}: current {c.Key} | previous {c.Value.PreviousDeltaDfsHash.ToByteArray().ToCid()}")));
            return chainedDeltas;
        }

        private void SetCacheExpectations(Dictionary<Cid, Delta> deltasByHash)
        {
            foreach (var delta in deltasByHash)
            {
                _deltaCache.TryGetOrAddConfirmedDelta(delta.Key, out Arg.Any<Delta>())
                   .Returns(ci =>
                   {
                       ci[1] = delta.Value;
                       return true;
                   });
            }
        }

        [Fact]
        public void CacheDeltasBetween_Should_Stop_When_One_Of_Deltas_Is_Missing()
        {
            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader, _deltaIndexService,
            Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            var chainSize = 5;
            var chain = BuildChainedDeltas(chainSize);
            SetCacheExpectations(chain);

            var hashes = chain.Keys.ToArray();
            var brokenChainIndex = 2;
            _deltaCache.TryGetOrAddConfirmedDelta(hashes[brokenChainIndex], out Arg.Any<Delta>())
               .Returns(false);
            _userOutput.WriteLine($"chain is broken for {hashes[brokenChainIndex]}, it cannot be found on Dfs.");

            var cachedHashes = sync.CacheDeltasBetween(hashes.First(),
                hashes.Last(), _cancellationToken).ToList();

            OutputCachedHashes(cachedHashes);

            cachedHashes.Count.Should().Be(chainSize - brokenChainIndex);
            hashes.TakeLast(chainSize - brokenChainIndex + 1).ToList().ForEach(h =>
            {
                _deltaCache.Received(1).TryGetOrAddConfirmedDelta(h,
                    out Arg.Any<Delta>(), _cancellationToken);
            });
        }

        private void OutputCachedHashes(List<Cid> cachedHashes)
        {
            _userOutput.WriteLine("cached hashes between: ");
            _userOutput.WriteLine(string.Join(", ", cachedHashes));
        }

        [Fact]
        public void CacheDeltasBetween_Should_Complete_When_LatestKnownDelta_Is_Found()
        {
            var sync = new Synchroniser(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaDfsReader, _deltaIndexService,
            Substitute.For<IDfsService>(), _hashProvider, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            var chainSize = 7;
            var chain = BuildChainedDeltas(chainSize);
            SetCacheExpectations(chain);

            var hashes = chain.Keys.ToArray();

            var latestHashIndex = 3;
            _userOutput.WriteLine($"Caching deltas between {hashes[latestHashIndex]} and {hashes.Last()}");
            var cachedHashes = sync.CacheDeltasBetween(hashes[latestHashIndex],
                hashes.Last(), _cancellationToken).ToList();

            var expectedResultLength = chainSize - latestHashIndex + 1;
            cachedHashes.Count.Should().Be(expectedResultLength);

            OutputCachedHashes(cachedHashes);

            cachedHashes.Should().BeEquivalentTo(hashes.TakeLast(expectedResultLength));

            hashes.TakeLast(expectedResultLength - 1).Reverse().ToList().ForEach(h =>
            {
                _deltaCache.Received(1).TryGetOrAddConfirmedDelta(h,
                    out Arg.Any<Delta>(), _cancellationToken);
            });
        }
    }
}
