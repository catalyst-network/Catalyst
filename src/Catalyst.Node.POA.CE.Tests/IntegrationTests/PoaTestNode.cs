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
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Mempool;
using Catalyst.Core.Modules.Rpc.Server;
using Catalyst.Core.Modules.Web3;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.Tests.IntegrationTests
{
    public class PoaTestNode : ICatalystNode, IDisposable
    {
        private readonly DevDfs _dfs;
        private readonly IMempool<TransactionBroadcastDao> _memPool;
        private readonly ICatalystNode _node;
        private readonly DirectoryInfo _nodeDirectory;
        private readonly PeerId _nodePeerId;
        private readonly IPeerSettings _nodeSettings;
        private readonly IPeerRepository _peerRepository;
        private readonly IRpcServerSettings _rpcSettings;
        private readonly ILifetimeScope _scope;
        private readonly ContainerProvider _containerProvider;

        public PoaTestNode(string name,
            IPrivateKey privateKey,
            IPeerSettings nodeSettings,
            IEnumerable<PeerId> knownPeerIds,
            IFileSystem parentTestFileSystem,
            ITestOutputHelper output)
        {
            Name = name;
            _nodeSettings = nodeSettings;

            _nodeDirectory = parentTestFileSystem.GetCatalystDataDir().SubDirectoryInfo(Name);
            var nodeFileSystem = Substitute.ForPartsOf<FileSystem>();
            nodeFileSystem.GetCatalystDataDir().Returns(_nodeDirectory);

            _rpcSettings = RpcSettingsHelper.GetRpcServerSettings(nodeSettings.Port + 100);
            _nodePeerId = nodeSettings.PeerId;

            var baseDfsFolder = Path.Combine(parentTestFileSystem.GetCatalystDataDir().FullName, "dfs");
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _dfs = new DevDfs(parentTestFileSystem, hashProvider, baseDfsFolder);

            _memPool = new Mempool(
                new TestMempoolRepository(new InMemoryRepository<TransactionBroadcastDao, string>()));
            _peerRepository = Substitute.For<IPeerRepository>();
            var peersInRepo = knownPeerIds.Select(p => new Peer
            {
                PeerId = p
            }).ToList();
            _peerRepository.AsQueryable().Returns(peersInRepo.AsQueryable());
            _peerRepository.GetAll().Returns(peersInRepo);
            _peerRepository.Get(Arg.Any<string>()).Returns(ci =>
            {
                return peersInRepo.First(p => p.DocumentId.Equals((string) ci[0]));
            });

            _containerProvider = new ContainerProvider(new[]
                {
                    Constants.NetworkConfigFile(NetworkType.Devnet),
                    Constants.SerilogJsonConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f)), parentTestFileSystem, output);

            Program.RegisterNodeDependencies(_containerProvider.ContainerBuilder,
                excludedModules: new List<Type> {typeof(ApiModule), typeof(RpcServerModule)}
            );
            _containerProvider.ConfigureContainerBuilder(true, true);
            OverrideContainerBuilderRegistrations();

            _scope = _containerProvider.Container.BeginLifetimeScope(Name);
            _node = _scope.Resolve<ICatalystNode>();

            var keyStore = _scope.Resolve<IKeyStore>();
            var keyRegistry = _scope.Resolve<IKeyRegistry>();
            keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, privateKey);

            keyStore.KeyStoreEncryptAsync(privateKey, nodeSettings.NetworkType, KeyRegistryTypes.DefaultKey)
               .ConfigureAwait(false).GetAwaiter()
               .GetResult();
        }

        public string Name { get; }

        public IConsensus Consensus => _node.Consensus;

        public async Task RunAsync(CancellationToken cancellationSourceToken)
        {
            await _node.RunAsync(cancellationSourceToken).ConfigureAwait(false);
        }

        public async Task StartSocketsAsync() { await _node.StartSocketsAsync(); }

        public void Dispose() { Dispose(true); }

        protected void OverrideContainerBuilderRegistrations()
        {
            _containerProvider.ContainerBuilder.RegisterInstance(new TestPasswordReader()).As<IPasswordReader>();
            _containerProvider.ContainerBuilder.RegisterInstance(_nodeSettings).As<IPeerSettings>();
            _containerProvider.ContainerBuilder.RegisterInstance(_rpcSettings).As<IRpcServerSettings>();
            _containerProvider.ContainerBuilder.RegisterInstance(_nodePeerId).As<PeerId>();
            _containerProvider.ContainerBuilder.RegisterInstance(_dfs).As<IDfs>();
            _containerProvider.ContainerBuilder.RegisterInstance(_memPool).As<IMempool<TransactionBroadcastDao>>();
            _containerProvider.ContainerBuilder.RegisterInstance(_peerRepository).As<IPeerRepository>();
            _containerProvider.ContainerBuilder.RegisterType<TestFileSystem>().As<IFileSystem>()
               .WithParameter("rootPath", _nodeDirectory.FullName);
            _containerProvider.ContainerBuilder.RegisterInstance(Substitute.For<IPeerDiscovery>()).As<IPeerDiscovery>();
            var keySigner = Substitute.For<IKeySigner>();
            keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default).ReturnsForAnyArgs(true);
            keySigner.CryptoContext.SignatureLength.Returns(64);
            _containerProvider.ContainerBuilder.RegisterInstance(keySigner).As<IKeySigner>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _scope?.Dispose();
            _peerRepository?.Dispose();
            _containerProvider?.Dispose();
        }
    }
}
