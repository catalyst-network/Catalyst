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
using Autofac.Core;
using Catalyst.Abstractions;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.IO.Observers;
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
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Modules.Ledger.Repository;
using Catalyst.Core.Modules.Mempool;
using Catalyst.Core.Modules.Mempool.Repositories;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Peer;
using NSubstitute;
using NUnit.Framework;
using SharpRepository.InMemoryRepository;
using Catalyst.Core.Lib.P2P.Repository;
using Nethermind.Db;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Modules.Authentication;
using Catalyst.Core.Modules.Consensus;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger;
using Catalyst.Core.Modules.P2P.Discovery.Hastings;
using Catalyst.Core.Modules.Rpc.Server;
using Catalyst.Core.Modules.Sync;
using Catalyst.Core.Modules.Web3;
using Catalyst.Modules.POA.Consensus;
using Catalyst.Modules.POA.P2P;
using Catalyst.Node.POA.CE;
using SharpRepository.Repository;

namespace Catalyst.TestUtils
{
    public sealed class PoaTestNode : ICatalystNode, IDisposable
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
        private readonly ContainerProvider _containerProvider;
        private readonly IDeltaByNumberRepository _deltaByNumber;

        public PoaTestNode(string name,
            IPrivateKey privateKey,
            IPeerSettings nodeSettings,
            IDfsService dfsService,
            IEnumerable<PeerId> knownPeerIds,
            IFileSystem parentTestFileSystem)
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

            _containerProvider = new ContainerProvider(new[]
                {
                    Constants.NetworkConfigFile(NetworkType.Devnet),
                    Constants.SerilogJsonConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f)), parentTestFileSystem, TestContext.CurrentContext);

            RegisterNodeDependencies(_containerProvider.ContainerBuilder,
                excludedModules: new List<Type>
                {
                    typeof(ApiModule),
                    typeof(RpcServerModule)
                }
            );
            _containerProvider.ConfigureContainerBuilder(true, true);
            OverrideContainerBuilderRegistrations();

            _scope = _containerProvider.Container.BeginLifetimeScope(Name);
            _node = _scope.Resolve<ICatalystNode>();

            var keyStore = _scope.Resolve<IKeyStore>();
            var keyRegistry = _scope.Resolve<IKeyRegistry>();
            keyRegistry.RemoveItemFromRegistry(KeyRegistryTypes.DefaultKey);
            keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, privateKey);

            keyStore.KeyStoreEncryptAsync(privateKey, nodeSettings.NetworkType, KeyRegistryTypes.DefaultKey)
               .ConfigureAwait(false).GetAwaiter()
               .GetResult();
        }
        
        public static void RegisterNodeDependencies(ContainerBuilder containerBuilder,
            List<IModule> extraModuleInstances = default,
            List<Type> excludedModules = default)
        {
            // core modules
            containerBuilder.RegisterType<CatalystNodePoa>().As<ICatalystNode>();
            containerBuilder.RegisterType<ConsoleUserOutput>().As<IUserOutput>();
            containerBuilder.RegisterType<ConsoleUserInput>().As<IUserInput>();

            // message handlers
            containerBuilder.RegisterAssemblyTypes(typeof(CoreLibProvider).Assembly)
                .AssignableTo<IP2PMessageObserver>().As<IP2PMessageObserver>();

            containerBuilder.RegisterAssemblyTypes(typeof(RpcServerModule).Assembly)
                .AssignableTo<IRpcRequestObserver>().As<IRpcRequestObserver>()
                .PublicOnly();

            // DAO MapperInitialisers
            containerBuilder.RegisterAssemblyTypes(typeof(CoreLibProvider).Assembly)
                .AssignableTo<IMapperInitializer>().As<IMapperInitializer>();
            containerBuilder.RegisterType<MapperProvider>().As<IMapperProvider>()
                .SingleInstance();

            var modulesToRegister = DefaultModulesByTypes
                .Where(p => excludedModules == null || !excludedModules.Contains(p.Key))
                .Select(p => p.Value())
                .Concat(extraModuleInstances ?? new List<IModule>());

            foreach (var module in modulesToRegister)
            {
                containerBuilder.RegisterModule(module);
            }
        }
        
        private static readonly Dictionary<Type, Func<IModule>> DefaultModulesByTypes =
            new Dictionary<Type, Func<IModule>>
            {
                {typeof(CoreLibProvider), () => new CoreLibProvider()},
                {typeof(MempoolModule), () => new MempoolModule()},
                {typeof(ConsensusModule), () => new ConsensusModule()},
                {typeof(SynchroniserModule), () => new SynchroniserModule()},
                {typeof(KvmModule), () => new KvmModule()},
                {typeof(LedgerModule), () => new LedgerModule()},
                {typeof(HashingModule), () => new HashingModule()},
                {typeof(DiscoveryHastingModule), () => new DiscoveryHastingModule()},
                {typeof(RpcServerModule), () => new RpcServerModule()},
                {typeof(BulletProofsModule), () => new BulletProofsModule()},
                {typeof(KeystoreModule), () => new KeystoreModule()},
                {typeof(KeySignerModule), () => new KeySignerModule()},
                {typeof(DfsModule), () => new DfsModule()},
                {typeof(AuthenticationModule), () => new AuthenticationModule()},
                {
                    typeof(ApiModule),
                    () => new ApiModule("http://*:5005", new List<string> {"Catalyst.Core.Modules.Web3", "Catalyst.Core.Modules.Dfs"})
                },
                {typeof(PoaConsensusModule), () => new PoaConsensusModule()},
                {typeof(PoaP2PModule), () => new PoaP2PModule()}
            };

        private string Name { get; }

        public IConsensus Consensus => _node.Consensus;

        public async Task RunAsync(CancellationToken cancellationSourceToken)
        {
            await _node.RunAsync(cancellationSourceToken).ConfigureAwait(false);
        }

        public async Task StartSocketsAsync() { await _node.StartSocketsAsync(); }

        public void Dispose() { Dispose(true); }

        private void OverrideContainerBuilderRegistrations()
        {
            var builder = _containerProvider.ContainerBuilder;

            builder.RegisterInstance(_deltaByNumber).As<IDeltaByNumberRepository>();
            builder.RegisterInstance(new MemDb()).As<IDb>().SingleInstance();
            builder.RegisterInstance(new InMemoryRepository<Account, string>()).As<IRepository<Account, string>>().SingleInstance();
            builder.RegisterType<InMemoryRepository<DeltaIndexDao, string>>().As<IRepository<DeltaIndexDao, string>>().SingleInstance();
            builder.RegisterInstance(new InMemoryRepository<TransactionReceipts, string>())
               .AsImplementedInterfaces();
            builder.RegisterInstance(new InMemoryRepository<TransactionToDelta, string>())
               .AsImplementedInterfaces();

            _containerProvider.ContainerBuilder.RegisterInstance(new TestPasswordReader()).As<IPasswordReader>();
            _containerProvider.ContainerBuilder.RegisterInstance(_nodeSettings).As<IPeerSettings>();
            _containerProvider.ContainerBuilder.RegisterInstance(_rpcSettings).As<IRpcServerSettings>();
            _containerProvider.ContainerBuilder.RegisterInstance(_nodePeerId).As<PeerId>();
            _containerProvider.ContainerBuilder.RegisterInstance(_memPool).As<IMempool<PublicEntryDao>>();
            _containerProvider.ContainerBuilder.RegisterInstance(_dfsService).As<IDfsService>();
            _containerProvider.ContainerBuilder.RegisterInstance(_peerRepository).As<IPeerRepository>();
            _containerProvider.ContainerBuilder.RegisterType<TestFileSystem>().As<IFileSystem>()
               .WithParameter("rootPath", _nodeDirectory.FullName);
            _containerProvider.ContainerBuilder.RegisterInstance(Substitute.For<IPeerDiscovery>()).As<IPeerDiscovery>();
        }

        private void Dispose(bool disposing)
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
