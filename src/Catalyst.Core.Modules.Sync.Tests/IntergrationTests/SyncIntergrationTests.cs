using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Ledger;
using Catalyst.Core.Modules.Consensus.Cycle;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.Node.POA.CE.Tests.IntegrationTests;
using Catalyst.TestUtils;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Sync.Tests.IntergrationTests
{
    public class SyncIntergrationTests : FileSystemBasedTest
    {
        private readonly CancellationTokenSource _endOfTestCancellationSource;
        private readonly ILifetimeScope _scope;
        private readonly List<PoaTestNode> _nodes;

        public SyncIntergrationTests(ITestOutputHelper output) : base(output)
        {
            var context = new FfiWrapper();

            var poaNodeDetails = Enumerable.Range(0, 2).Select(i =>
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
                    return new {index = i, name, privateKey, nodeSettings, peerIdentifier, dfs, fileSystem};
                }
            ).ToList();

            var peerIdentifiers = poaNodeDetails.Select(n => n.peerIdentifier).ToList();

            _nodes = new List<PoaTestNode>();
            foreach (var nodeDetails in poaNodeDetails)
            {
                nodeDetails.dfs.Options.Discovery.BootstrapPeers = poaNodeDetails.Except(new[] {nodeDetails})
                   .Select(x => x.dfs.LocalPeer.Addresses.First());
                var node = new PoaTestNode(nodeDetails.name,
                    nodeDetails.privateKey,
                    nodeDetails.nodeSettings,
                    nodeDetails.dfs,
                    peerIdentifiers.Except(new[] {nodeDetails.peerIdentifier}),
                    nodeDetails.fileSystem,
                    output);

                _nodes.Add(node);
            }
        }

        [Fact]
        public async Task Can_Sync_From_Another_Node()
        {
            var poaTestNode1 = _nodes[0];
            var ledger = poaTestNode1._containerProvider.Container.Resolve<ILedger>();

            Task.Run(() =>
            {
                poaTestNode1.RunAsync(_endOfTestCancellationSource.Token);
                poaTestNode1.Consensus.StartProducing();
            });

            await Task.Delay(Debugger.IsAttached ? TimeSpan.FromHours(3) : CycleConfiguration.Default.CycleDuration.Multiply(2.3)).ConfigureAwait(false);

            var poaTestNode2 = _nodes[1];
        }
    }
}
