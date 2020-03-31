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
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Ledger.Models;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.Rpc;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Core.Modules.Mempool;
using Catalyst.Core.Modules.Mempool.Repositories;
using Catalyst.Core.Modules.Rpc.Server;
using Catalyst.Core.Modules.Web3;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using NSubstitute;
using SharpRepository.InMemoryRepository;
using Xunit.Abstractions;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.DAO.Ledger;
using SharpRepository.Repository;
using Nethermind.Store;

namespace Catalyst.Node.POA.CE.Tests.IntegrationTests
{
    public class PoaTestNode : ICatalystNode, IDisposable
    {
        private readonly IDfsService _dfsService;
        private readonly IMempool<PublicEntryDao> _memPool;
        private readonly ICatalystNode _node;
        private readonly DirectoryInfo _nodeDirectory;
        private readonly PeerId _nodePeerId;
        private readonly IPeerSettings _nodeSettings;
        private readonly IPeerRepository _peerRepository;
        private readonly IRpcServerSettings _rpcSettings;
        private readonly ILifetimeScope _scope;
        public readonly ContainerProvider ContainerProvider;
        private readonly IDeltaByNumberRepository _deltaByNumber;

        public PoaTestNode(string name,
            IPrivateKey privateKey,
            IPeerSettings nodeSettings,
            IDfsService dfsService,
            IEnumerable<PeerId> knownPeerIds,
            IFileSystem parentTestFileSystem,
            ITestOutputHelper output)
        {
            Name = name;
            _nodeSettings = nodeSettings;

            _nodeDirectory = parentTestFileSystem.GetCatalystDataDir();

            _dfsService = dfsService;

            _rpcSettings = RpcSettingsHelper.GetRpcServerSettings(nodeSettings.Port + 100);
            _nodePeerId = nodeSettings.PeerId;

            _memPool = new Mempool(new MempoolService(new InMemoryRepository<PublicEntryDao, string>()));
            _peerRepository = new PeerRepository(new InMemoryRepository<Peer, string>());
            var peersInRepo = knownPeerIds.Select(p => new Peer
            {
                PeerId = p,
                IsPoaNode = true,
                LastSeen = DateTime.UtcNow
            }).ToList();
            _peerRepository.Add(peersInRepo);

            _deltaByNumber = new DeltaByNumberRepository(new InMemoryRepository<DeltaByNumber, string>());

            ContainerProvider = new ContainerProvider(new[]
                {
                    Constants.NetworkConfigFile(NetworkType.Devnet),
                    Constants.SerilogJsonConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f)), parentTestFileSystem, output);

            Program.RegisterNodeDependencies(ContainerProvider.ContainerBuilder,
                excludedModules: new List<Type>
                {
                    typeof(ApiModule),
                    typeof(RpcServerModule)
                }
            );
            ContainerProvider.ConfigureContainerBuilder(true, true);
            OverrideContainerBuilderRegistrations();

            _scope = ContainerProvider.Container.BeginLifetimeScope(Name);
            _node = _scope.Resolve<ICatalystNode>();

            var keyStore = _scope.Resolve<IKeyStore>();
            var keyRegistry = _scope.Resolve<IKeyRegistry>();
            keyRegistry.RemoveItemFromRegistry(KeyRegistryTypes.DefaultKey);
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
            var builder = ContainerProvider.ContainerBuilder;

            builder.RegisterInstance(_deltaByNumber).As<IDeltaByNumberRepository>();
            builder.RegisterInstance(new MemDb()).As<IDb>().SingleInstance();
            builder.RegisterInstance(new StateDb()).As<ISnapshotableDb>().SingleInstance();
            builder.RegisterInstance(new InMemoryRepository<Account, string>()).As<IRepository<Account, string>>().SingleInstance();
            builder.RegisterType<InMemoryRepository<DeltaIndexDao, string>>().As<IRepository<DeltaIndexDao, string>>().SingleInstance();
            builder.RegisterInstance(new InMemoryRepository<TransactionReceipts, string>())
               .AsImplementedInterfaces();
            builder.RegisterInstance(new InMemoryRepository<TransactionToDelta, string>())
               .AsImplementedInterfaces();

            ContainerProvider.ContainerBuilder.RegisterInstance(new TestPasswordReader()).As<IPasswordReader>();
            ContainerProvider.ContainerBuilder.RegisterInstance(_nodeSettings).As<IPeerSettings>();
            ContainerProvider.ContainerBuilder.RegisterInstance(_rpcSettings).As<IRpcServerSettings>();
            ContainerProvider.ContainerBuilder.RegisterInstance(_nodePeerId).As<PeerId>();
            ContainerProvider.ContainerBuilder.RegisterInstance(_memPool).As<IMempool<PublicEntryDao>>();
            ContainerProvider.ContainerBuilder.RegisterInstance(_dfsService).As<IDfsService>();
            ContainerProvider.ContainerBuilder.RegisterInstance(_peerRepository).As<IPeerRepository>();
            ContainerProvider.ContainerBuilder.RegisterType<TestFileSystem>().As<IFileSystem>()
               .WithParameter("rootPath", _nodeDirectory.FullName);
            ContainerProvider.ContainerBuilder.RegisterInstance(Substitute.For<IPeerDiscovery>()).As<IPeerDiscovery>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _scope?.Dispose();
            _peerRepository?.Dispose();
            ContainerProvider?.Dispose();
        }
    }
}
