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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Sync.Manager;
using Catalyst.Core.Modules.Sync.Watcher;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using LibP2P = Lib.P2P;
using FluentAssertions;
using Google.Protobuf;
using Lib.P2P;
using MultiFormats.Registry;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using NUnit.Framework;
using MultiFormats;
using Catalyst.Abstractions.Dfs.CoreApi;
using System.Reactive.Concurrency;
using NUnit.Framework.Internal;
using ILogger = Serilog.ILogger;

namespace Catalyst.Core.Modules.Sync.Tests.UnitTests
{
    public class SyncUnitTests
    {
        private IHashProvider _hashProvider;
        private IPeerSettings _peerSettings;
        private IPeerClient _peerClient;
        private IDeltaIndexService _deltaIndexService;

        private ReplaySubject<ProtocolMessage> _deltaHeightReplaySubject;

        private ReplaySubject<ProtocolMessage> _deltaHistoryReplaySubject;

        private IMapperProvider _mapperProvider;
        private IUserOutput _userOutput;

        private IDeltaCache _deltaCache;

        private IPeerSyncManager _peerSyncManager;
        private IDeltaHeightWatcher _deltaHeightWatcher;

        private IDeltaHashProvider _deltaHashProvider;

        private int _syncTestHeight = 1005;

        private ManualResetEventSlim _manualResetEventSlim;

        private CancellationToken _cancellationToken;

        [SetUp]
        public void Init()
        {
            var peerService = Substitute.For<IPeerService>();

            List<LibP2P.Peer> peers = new();
            Enumerable.Range(0, 5).Select(x => MultiAddressHelper.GetAddress(x.ToString(), port: x)).Select(x => new LibP2P.Peer { Id = x.PeerId, ConnectedAddress = x }).ToList().ForEach(peers.Add);

            var swarmApi = Substitute.For<ISwarmApi>();
            swarmApi.PeersAsync().Returns(peers);

            _cancellationToken = new CancellationToken();

            _manualResetEventSlim = new ManualResetEventSlim(false);

            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));

            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.Address.Returns(MultiAddressHelper.GetAddress());

            _deltaCache = Substitute.For<IDeltaCache>();
            _deltaCache.GenesisHash.Returns("bafk2bzacecji5gcdd6lxsoazgnbg46c3vttjwwkptiw27enachziizhhkir2w".ToCid());

            _deltaHashProvider = new DeltaHashProvider(_deltaCache, Substitute.For<IDeltaIndexService>(), Substitute.For<ILogger>());

            _deltaIndexService = new DeltaIndexService(new InMemoryRepository<DeltaIndexDao, string>());
            _deltaIndexService.Add(new DeltaIndexDao { Cid = _hashProvider.ComputeUtf8MultiHash("0").ToCid(), Height = 0 });

            _peerClient = Substitute.For<IPeerClient>();
            ModifyPeerClient<LatestDeltaHashRequest>((request, senderentifier) =>
            {
                var deltaHeightResponse = new LatestDeltaHashResponse
                {
                    DeltaIndex = new DeltaIndex
                    {
                        Cid = _hashProvider.ComputeUtf8MultiHash(_syncTestHeight.ToString()).ToCid().ToArray()
                           .ToByteString(),
                        Height = (uint) _syncTestHeight
                    }
                };

                _deltaHeightReplaySubject.OnNext(deltaHeightResponse.ToProtocolMessage(senderentifier, CorrelationId.GenerateCorrelationId()));
            });

            ModifyPeerClient<DeltaHistoryRequest>((request, senderentifier) =>
            {
                var data = GenerateSampleData((int) request.Height, (int) request.Range, (int) _syncTestHeight);
                _deltaIndexService.Add(data.DeltaIndex.Select(x => DeltaIndexDao.ToDao<DeltaIndex>(x, _mapperProvider)));

                _deltaHistoryReplaySubject.OnNext(data.ToProtocolMessage(senderentifier, CorrelationId.GenerateCorrelationId()));
            });

            _deltaHeightReplaySubject = new ReplaySubject<ProtocolMessage>(1);
            _deltaHistoryReplaySubject = new ReplaySubject<ProtocolMessage>(1);

            var mergeMessageStreams = _deltaHeightReplaySubject.AsObservable()
               .Merge(_deltaHistoryReplaySubject.AsObservable());

            peerService.MessageStream.Returns(mergeMessageStreams);

            _deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            _deltaHashProvider.TryUpdateLatestHash(Arg.Any<Cid>(), Arg.Any<Cid>()).Returns(true);

            _mapperProvider = new TestMapperProvider();

            _userOutput = Substitute.For<IUserOutput>();

            _deltaHeightWatcher = new DeltaHeightWatcher(_peerClient, swarmApi, peerService);

            var dfsService = Substitute.For<IDfsService>();

            _peerSyncManager = new PeerSyncManager(_peerClient, peerService, _userOutput, _deltaHeightWatcher, swarmApi, Substitute.For<ILogger>(), 0.7, 0);
        }

        [TearDown]
        public void TearDown()
        {
            _deltaIndexService.Dispose();
            _deltaHeightWatcher.Dispose();
            _peerSyncManager.Dispose();
        }

        private DeltaHistoryResponse GenerateSampleData(int height, int range, int maxHeight = -1)
        {
            DeltaHistoryResponse deltaHeightResponse = new();
            List<DeltaIndex> deltaIndexList = new();
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
                    Height = (uint) i
                });
            }

            deltaHeightResponse.DeltaIndex.Add(deltaIndexList);
            return deltaHeightResponse;
        }

        private void ModifyPeerClient<TRequest>(Action<TRequest, MultiAddress> callback) where TRequest : IMessage<TRequest>
        {
            _peerClient.When(x =>
                x.SendMessageToPeersAsync(
                    Arg.Is<TRequest>(y => y.Descriptor.ClrType.Name.EndsWith(typeof(TRequest).Name)), Arg.Any<IEnumerable<MultiAddress>>())).Do(
                z =>
                {
                    var request = (TRequest) z[0];
                    var peers = (IEnumerable<MultiAddress>) z[1];
                    foreach (var peer in peers)
                    {
                        callback.Invoke(request, peer);
                    }
                });
        }

        [Test]
        public async Task StartAsync_Should_Start_Sync()
        {
            _deltaHeightWatcher = Substitute.For<IDeltaHeightWatcher>();
            _deltaHeightWatcher.WaitForDeltaIndexAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(new DeltaIndex { Cid = ByteString.Empty, Height = 10000 });

            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider,
                _deltaIndexService, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StartAsync(CancellationToken.None);

            sync.IsRunning.Should().BeTrue();
        }

        [Test]
        public async Task StopAsync_Should_Stop_Sync()
        {
            _deltaHeightWatcher = Substitute.For<IDeltaHeightWatcher>();
            _deltaHeightWatcher.WaitForDeltaIndexAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(new DeltaIndex { Cid = ByteString.Empty, Height = 10000 });

            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider,
                _deltaIndexService, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StartAsync(CancellationToken.None);
            sync.IsRunning.Should().BeTrue();

            await sync.StopAsync(CancellationToken.None);
            sync.IsRunning.Should().BeFalse();
        }

        [Test]
        public async Task StopAsync_Should_Log_If_Not_Running_Sync()
        {
            _deltaHeightWatcher = Substitute.For<IDeltaHeightWatcher>();
            _deltaHeightWatcher.GetHighestDeltaIndexAsync().Returns(new DeltaIndex { Cid = ByteString.Empty, Height = 10000 });

            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider,
                _deltaIndexService, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StopAsync(CancellationToken.None);

            _userOutput.Received(1).WriteLine("Sync is not currently running.");

            sync.IsRunning.Should().BeFalse();
        }

        [Test]
        public async Task Can_Restore_DeltaIndex()
        {
            const int sampleCurrentDeltaHeight = 100;

            var cid = _hashProvider.ComputeUtf8MultiHash(sampleCurrentDeltaHeight.ToString()).ToCid();
            _deltaIndexService.Add(new DeltaIndexDao { Cid = cid, Height = sampleCurrentDeltaHeight });

            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, Substitute.For<IDeltaHeightWatcher>(), _deltaHashProvider,
                _deltaIndexService, _mapperProvider, _userOutput, Substitute.For<ILogger>());

            await sync.StartAsync(CancellationToken.None);

            sync.CurrentHighestDeltaIndexStored.Should().Be(sampleCurrentDeltaHeight);
        }

        [Test]
        public async Task Sync_Can_Add_DeltaIndexRange_To_Repository()
        {
            _syncTestHeight = 10;
            var expectedData = GenerateSampleData(0, _syncTestHeight, _syncTestHeight);
            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaIndexService,
                _mapperProvider, _userOutput, Substitute.For<ILogger>(), _syncTestHeight, 1, 30, Scheduler.Default);

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            var range = _deltaIndexService.GetRange(0, (ulong) _syncTestHeight).Select(x => DeltaIndexDao.ToProtoBuff<DeltaIndex>(x, _mapperProvider));
            range.Should().BeEquivalentTo(expectedData.DeltaIndex);
        }

        [Test]
        public async Task Sync_Can_Update_State()
        {
            _syncTestHeight = 10;

            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaIndexService,
                _mapperProvider, _userOutput, Substitute.For<ILogger>(), _syncTestHeight, 1, 30, Scheduler.Default);

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            _deltaHashProvider.Received(_syncTestHeight).TryUpdateLatestHash(Arg.Any<Cid>(), Arg.Any<Cid>());
        }

        [Test]
        public async Task Sync_Can_Update_CurrentDeltaIndex_From_Requested_DeltaIndexRange()
        {
            _syncTestHeight = 10;

            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaIndexService,
                _mapperProvider, _userOutput, Substitute.For<ILogger>());

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            sync.CurrentHighestDeltaIndexStored.Should().Be(10);
        }

        [Test]
        public async Task Sync_Can_Complete()
        {
            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaIndexService,
                _mapperProvider, _userOutput, Substitute.For<ILogger>());

            sync.SyncCompleted.Subscribe(x => { _manualResetEventSlim.Set(); });

            await sync.StartAsync(CancellationToken.None);

            _manualResetEventSlim.Wait();

            sync.CurrentHighestDeltaIndexStored.Should().Be((ulong) _syncTestHeight);
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

        [Test]
        public void CacheDeltasBetween_Should_Stop_When_One_Of_Deltas_Is_Missing()
        {
            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaIndexService,
            _mapperProvider, _userOutput, Substitute.For<ILogger>());

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

        [Test]
        public void CacheDeltasBetween_Should_Complete_When_LatestKnownDelta_Is_Found()
        {
            Synchroniser sync = new(new SyncState(), _peerSyncManager, _deltaCache, _deltaHeightWatcher, _deltaHashProvider, _deltaIndexService,
            _mapperProvider, _userOutput, Substitute.For<ILogger>());

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
