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
using Autofac;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Dfs.Migration;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using MultiFormats.Registry;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Catalyst.TestUtils
{
    public class TestDfs
    {
        private sealed class TestDfsFileSystem : FileSystemBasedTest
        {
        }

        public static IDfsService GetTestDfs(IFileSystem fileSystem = default, string hashName = "keccak-256", string keyType = null)
        {
            var nodeGuid = Guid.NewGuid();
            var containerBuilder = new ContainerBuilder();

            if (fileSystem == null)
            {
                var testFileSystem = new TestDfsFileSystem();
                testFileSystem.Setup(TestContext.CurrentContext);
                fileSystem = testFileSystem.FileSystem;
            }

            containerBuilder.RegisterModule<CoreLibProvider>();
            containerBuilder.RegisterModule<KeystoreModule>();
            containerBuilder.RegisterInstance(new PasswordManager(new TestPasswordReader(), new PasswordRegistry())).As<IPasswordManager>().SingleInstance();
            containerBuilder.RegisterInstance(fileSystem).As<IFileSystem>();
            containerBuilder.RegisterType<MigrationManager>().As<IMigrationManager>();
            containerBuilder.RegisterModule<HashingModule>();
            containerBuilder.RegisterInstance(new HashProvider(HashingAlgorithm.GetAlgorithmMetadata(hashName))).As<IHashProvider>();
            containerBuilder.RegisterType<KeyStoreService>().As<IKeyStoreService>().SingleInstance();
            containerBuilder.RegisterModule(new DfsModule());
            containerBuilder.RegisterType<DiscoveryOptions>().SingleInstance().WithProperty("DisableMdns", true).WithProperty("UsePeerRepository", false);
            if (keyType != null)
            {
                containerBuilder.RegisterType<KeyChainOptions>().SingleInstance().WithProperty("DefaultKeyType", keyType);
            }

            var container = containerBuilder.Build();
            var scope = container.BeginLifetimeScope(nodeGuid);
            var dfsService = scope.Resolve<IDfsService>();

            dfsService.ConfigApi.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new[]
                {
                    "/ip4/0.0.0.0/tcp/0"
                })
            ).Wait();

            return dfsService;
        }
    }
}
