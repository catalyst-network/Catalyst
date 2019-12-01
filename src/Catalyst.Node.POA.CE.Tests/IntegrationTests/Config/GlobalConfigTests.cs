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
using System.IO;
using System.Linq;
using Autofac;
using Catalyst.Abstractions;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.DAO;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Modules.Authentication;
using Catalyst.Core.Modules.Consensus;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Core.Modules.Ledger;
using Catalyst.Core.Modules.Mempool;
using Catalyst.Core.Modules.Rpc.Server;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.Tests.IntegrationTests.Config
{
    public class GlobalConfigTests : FileSystemBasedTest
    {
        public static readonly List<object[]> Networks =
            new List<NetworkType> {NetworkType.Devnet, NetworkType.Mainnet, NetworkType.Testnet}
               .Select(n => new object[] {n}).ToList();

        private IEnumerable<string> _configFilesUsed;
        private ContainerProvider _containerProvider;

        public GlobalConfigTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [MemberData(nameof(Networks))]
        public void Registering_All_Configs_Should_Allow_Resolving_CatalystNode(NetworkType network)
        {
            _configFilesUsed = new[]
                {
                    Constants.NetworkConfigFile(network),
                    Constants.SerilogJsonConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f));

            _containerProvider = new ContainerProvider(_configFilesUsed, FileSystem, Output);

            SocketPortHelper.AlterConfigurationToGetUniquePort(_containerProvider.ConfigurationRoot);

            _containerProvider.ConfigureContainerBuilder();

            var containerBuilder = _containerProvider.ContainerBuilder;
            containerBuilder.RegisterType<CatalystNodePoa>().As<ICatalystNode>();
            containerBuilder.RegisterType<ConsoleUserOutput>().As<IUserOutput>();
            containerBuilder.RegisterType<ConsoleUserInput>().As<IUserInput>();
            containerBuilder.RegisterInstance(Substitute.For<IPeerDiscovery>()).As<IPeerDiscovery>();
            containerBuilder.RegisterModule(new KeySignerModule());
            containerBuilder.RegisterModule(new ConsensusModule());
            containerBuilder.RegisterModule(new DfsModule());
            containerBuilder.RegisterModule(new KvmModule());
            containerBuilder.RegisterModule(new LedgerModule());
            containerBuilder.RegisterModule(new RpcServerModule());
            containerBuilder.RegisterModule(new MempoolModule());
            containerBuilder.RegisterModule(new KeystoreModule());
            containerBuilder.RegisterModule(new BulletProofsModule());
            containerBuilder.RegisterModule(new AuthenticationModule());

            containerBuilder.RegisterAssemblyTypes(typeof(CoreLibProvider).Assembly)
               .AssignableTo<IMapperInitializer>().As<IMapperInitializer>();
            containerBuilder.RegisterType<MapperProvider>().As<IMapperProvider>()
               .SingleInstance();

            using (var scope = _containerProvider.Container.BeginLifetimeScope(CurrentTestName + network))
            {
                _ = scope.Resolve<ICatalystNode>();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _containerProvider?.Dispose();
        }
    }
}
