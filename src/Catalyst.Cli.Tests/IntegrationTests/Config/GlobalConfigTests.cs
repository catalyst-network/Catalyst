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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Cli.Commands;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Core.Modules.Rpc.Client;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.IntegrationTests.Config
{
    public sealed class GlobalConfigTests : FileSystemBasedTest
    {
        public static readonly List<object[]> Networks =
            new List<NetworkType> {NetworkType.Devnet, NetworkType.Mainnet, NetworkType.Testnet}
               .Select(n => new object[] {n}).ToList();

        public GlobalConfigTests(TestContext output) : base(output) { }

        [Theory]
        [TestCase(nameof(Networks))]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Registering_All_Configs_Should_Allow_Resolving_ICatalystCli(NetworkType network)
        {
            var configFilesUsed = new[]
                {
                    Constants.NetworkConfigFile(network),
                    Constants.SerilogJsonConfigFile,
                    CliConstants.ShellNodesConfigFile,
                    CliConstants.ShellConfigFile
                }
               .Select(f => Path.Combine(Constants.ConfigSubFolder, f));

            using (var containerProvider = new ContainerProvider(configFilesUsed, FileSystem, Output))
            {
                var containerBuilder = containerProvider.ContainerBuilder;
                containerBuilder.RegisterModule(new CoreLibProvider());
                containerBuilder.RegisterModule(new KeystoreModule());
                containerBuilder.RegisterModule(new KeySignerModule());
                containerBuilder.RegisterModule(new BulletProofsModule());
                containerBuilder.RegisterModule(new RpcClientModule());

                containerBuilder.RegisterType<ConsoleUserOutput>().As<IUserOutput>();
                containerBuilder.RegisterType<CatalystCli>().As<ICatalystCli>();
                containerBuilder.RegisterType<ConsoleUserInput>().As<IUserInput>();
                containerBuilder.RegisterType<CommandContext>().As<ICommandContext>();

                containerProvider.ConfigureContainerBuilder();

                using (var scope = containerProvider.Container.BeginLifetimeScope(CurrentTestName + network))
                {
                    scope.Resolve<ICatalystCli>();
                }
            }
        }
    }
}
