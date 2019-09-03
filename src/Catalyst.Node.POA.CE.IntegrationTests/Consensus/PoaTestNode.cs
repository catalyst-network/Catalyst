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
using Catalyst.Abstractions;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.Rpc;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.Dfs;
using Catalyst.Core.Extensions;
using Catalyst.Core.FileSystem;
using Catalyst.Core.Mempool;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Core.P2P;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.TestUtils;
using Ipfs.Registry;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.IntegrationTests.Consensus
{
    public class PoaTestNode : ICatalystNode, IDisposable
    {
        private readonly ContainerProvider _containerProvider;
        private readonly DevDfs _dfs;
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly ICatalystNode _node;
        private readonly DirectoryInfo _nodeDirectory;
        private readonly IPeerIdentifier _nodePeerId;
        private readonly IPeerSettings _nodeSettings;
        private readonly IPeerRepository _peerRepository;
        private readonly IRpcServerSettings _rpcSettings;
        private readonly ILifetimeScope _scope;

        public PoaTestNode(string name,
            IPrivateKey privateKey,
            IPeerSettings nodeSettings,
            IEnumerable<IPeerIdentifier> knownPeerIds,
            IFileSystem parentTestFileSystem,
            ITestOutputHelper output)
        {
            Name = name;
            _nodeSettings = nodeSettings;

            _nodeDirectory = parentTestFileSystem.GetCatalystDataDir().SubDirectoryInfo(Name);
            var nodeFileSystem = Substitute.ForPartsOf<FileSystem>();
            nodeFileSystem.GetCatalystDataDir().Returns(_nodeDirectory);

            _rpcSettings = RpcServerSettingsHelper.GetRpcServerSettings(nodeSettings.Port + 100);
            _nodePeerId = new PeerIdentifier(nodeSettings);

            var baseDfsFolder = Path.Combine(parentTestFileSystem.GetCatalystDataDir().FullName, "dfs");
            var hashingAlgorithm = HashingAlgorithm.All.First(x => x.Name == "blake2b-256");
            _dfs = new DevDfs(parentTestFileSystem, hashingAlgorithm, baseDfsFolder);

            _mempool = new Mempool(new TestMempoolDocumentRepository(new InMemoryRepository<MempoolDocument, string>()), Substitute.For<ILogger>());
            _peerRepository = Substitute.For<IPeerRepository>();
            var peersInRepo = knownPeerIds.Select(p => new Peer {PeerIdentifier = p}).ToList();
            _peerRepository.AsQueryable().Returns(peersInRepo.AsQueryable());
            _peerRepository.GetAll().Returns(peersInRepo);
            _peerRepository.Get(Arg.Any<string>()).Returns(ci =>
            {
                return peersInRepo.First(p => p.DocumentId.Equals((string) ci[0]));
            });

            _containerProvider = new ContainerProvider(new[]
                {
                    Constants.NetworkConfigFile(Network.Devnet),
                    Constants.ComponentsJsonConfigFile,
                    Constants.SerilogJsonConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f)), parentTestFileSystem, output);

            _containerProvider.ConfigureContainerBuilder(true, true);
            OverrideContainerBuilderRegistrations();

            _scope = _containerProvider.Container.BeginLifetimeScope(Name);
            _node = _scope.Resolve<ICatalystNode>();

            var keyStore = _scope.Resolve<IKeyStore>();
            var keyRegistry = _scope.Resolve<IKeyRegistry>();
            keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, privateKey);

            keyStore.KeyStoreEncryptAsync(privateKey, KeyRegistryTypes.DefaultKey).ConfigureAwait(false).GetAwaiter()
               .GetResult();
        }

        public string Name { get; }

        public IConsensus Consensus => _node.Consensus;

        public async Task RunAsync(CancellationToken cancellationSourceToken)
        {
            await _node.RunAsync(cancellationSourceToken).ConfigureAwait(false);
        }

        public async Task StartSockets() { await _node.StartSockets(); }

        public void Dispose() { Dispose(true); }

        protected void OverrideContainerBuilderRegistrations()
        {
            _containerProvider.ContainerBuilder.RegisterInstance(new TestPasswordReader()).As<IPasswordReader>();
            _containerProvider.ContainerBuilder.RegisterInstance(_nodeSettings).As<IPeerSettings>();
            _containerProvider.ContainerBuilder.RegisterInstance(_rpcSettings).As<IRpcServerSettings>();
            _containerProvider.ContainerBuilder.RegisterInstance(_nodePeerId).As<IPeerIdentifier>();
            _containerProvider.ContainerBuilder.RegisterInstance(_dfs).As<IDfs>();
            _containerProvider.ContainerBuilder.RegisterInstance(_mempool).As<IMempool<MempoolDocument>>();
            _containerProvider.ContainerBuilder.RegisterInstance(_peerRepository).As<IPeerRepository>();
            _containerProvider.ContainerBuilder.RegisterType<TestFileSystem>().As<IFileSystem>()
               .WithParameter("rootPath", _nodeDirectory.FullName);
            _containerProvider.ContainerBuilder.RegisterInstance(new NoDiscovery()).As<IPeerDiscovery>();
            var keySigner = Substitute.For<IKeySigner>();
            keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default).ReturnsForAnyArgs(true);
            _containerProvider.ContainerBuilder.RegisterInstance(keySigner).As<IKeySigner>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _scope?.Dispose();
            _peerRepository?.Dispose();
            _containerProvider?.Dispose();
        }
    }
}
