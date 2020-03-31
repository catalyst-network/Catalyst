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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.Node.POA.CE.Tests.IntegrationTests;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Core.Crypto;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Sync.Tests.IntegrationTests
{
    public class SyncIntegrationTests : FileSystemBasedTest
    {
        private readonly CancellationTokenSource _endOfTestCancellationSource;
        private readonly ILifetimeScope _scope;
        private readonly List<PoaTestNode> _nodes;

        public SyncIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _endOfTestCancellationSource = new CancellationTokenSource();

            var context = new FfiWrapper();

            var poaNodeDetails = Enumerable.Range(0, 3).Select(i =>
                {
                    var fileSystem = Substitute.For<IFileSystem>();
                    var path = Path.Combine(FileSystem.GetCatalystDataDir().FullName, $"producer{i}");
                    fileSystem.GetCatalystDataDir().Returns(new DirectoryInfo(path));

                    var privateKey = context.GeneratePrivateKey();
                    var publicKey = privateKey.GetPublicKey();
                    var nodeSettings = PeerSettingsHelper.TestPeerSettings(publicKey.Bytes, 2000 + i);
                    var peerIdentifier = nodeSettings.PeerId;
                    var name = $"producer{i.ToString()}";
                    var dfs = TestDfs.GetTestDfs(output, fileSystem);
                    return new { index = i, name, privateKey, nodeSettings, peerIdentifier, dfs, fileSystem };
                }
            ).ToList();

            var peerIdentifiers = poaNodeDetails.Select(n => n.peerIdentifier).ToList();

            _nodes = new List<PoaTestNode>();
            foreach (var nodeDetails in poaNodeDetails)
            {
                nodeDetails.dfs.Options.Discovery.BootstrapPeers = poaNodeDetails.Except(new[] { nodeDetails })
                   .Select(x => x.dfs.LocalPeer.Addresses.First());
                var node = new PoaTestNode(nodeDetails.name,
                    nodeDetails.privateKey,
                    nodeDetails.nodeSettings,
                    nodeDetails.dfs,
                    peerIdentifiers.Except(new[] { nodeDetails.peerIdentifier }),
                    nodeDetails.fileSystem,
                    output);

                _nodes.Add(node);
            }
        }

        //todo
        //[Fact]
        //public async Task Can_Sync_From_Another_Nodes()
        //{
        //    var manualResetEvent = new ManualResetEvent(false);
        //    var tasks = _nodes.Count();
        //    var utcNow = DateTime.UtcNow;
        //    var cids = new List<string>();
        //    Delta previousDelta = null;
        //    for (var j = 0; j < _nodes.Count(); j++)
        //    {
        //        var nodeJ = _nodes[j];
        //        var ledger = nodeJ.ContainerProvider.Container.Resolve<ILedger>();
        //        var deltaHashProvider = nodeJ.ContainerProvider.Container.Resolve<IDeltaHashProvider>();
        //        var hashProvider = nodeJ.ContainerProvider.Container.Resolve<IHashProvider>();
        //        var dfsService = nodeJ.ContainerProvider.Container.Resolve<IDfsService>();
        //        var deltaCache = nodeJ.ContainerProvider.Container.Resolve<IDeltaCache>();
        //        var sync = nodeJ.ContainerProvider.Container.Resolve<ISynchroniser>();
        //        sync.SyncCompleted.Subscribe(x =>
        //        {
        //            tasks--;
        //            if (tasks <= 0)
        //            {
        //                _endOfTestCancellationSource.Cancel();
        //                manualResetEvent.Set();
        //            }
        //        });

        //        if (previousDelta == null)
        //        {
        //            deltaCache.TryGetOrAddConfirmedDelta(deltaCache.GenesisHash, out previousDelta);
        //        }

        //        for (var i = 1; i < 25; i++)
        //        {
        //            if (j == 2)
        //            {
        //                break;
        //            }

        //            if (j == 1 && i > 15)
        //            {
        //                break;
        //            }

        //            var delta = new Delta
        //            {
        //                StateRoot = previousDelta.StateRoot.ToByteString(),
        //                PreviousDeltaDfsHash = ledger.LatestKnownDelta.ToArray().ToByteString(),
        //                MerkleRoot = hashProvider.ComputeMultiHash(ledger.LatestKnownDelta.ToArray()).ToCid().ToArray()
        //                   .ToByteString(),
        //                TimeStamp = Timestamp.FromDateTime(utcNow.AddMilliseconds(i + 1))
        //            };

        //            var node = await dfsService.UnixFsApi.AddAsync(delta.ToByteArray().ToMemoryStream(), string.Empty,
        //                    new AddFileOptions { Hash = hashProvider.HashingAlgorithm.Name }, CancellationToken.None)
        //               .ConfigureAwait(false);

        //            cids.Add(node.Id.Hash.ToBase32());

        //            deltaHashProvider.TryUpdateLatestHash(ledger.LatestKnownDelta, node.Id);
        //        }
        //    }

        //    Task.Run(() => _nodes[0].RunAsync(_endOfTestCancellationSource.Token));
        //    Task.Run(() => _nodes[1].RunAsync(_endOfTestCancellationSource.Token));
        //    Task.Run(() => _nodes[2].RunAsync(_endOfTestCancellationSource.Token));

        //    manualResetEvent.WaitOne(TimeSpan.FromMinutes(5));
        //}
    }
}
