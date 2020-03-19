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
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Modules.Dfs.Migration;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using MultiFormats.Registry;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.Utils
{
    public class TestDfs
    {
        private sealed class TestDfsFileSystem : FileSystemBasedTest
        {
            internal TestDfsFileSystem() : base(TestContext.CurrentContext) { }
        }

        public static IDfsService GetTestDfs(IFileSystem fileSystem = default, string hashName = "blake2b-256")
        {
            var nodeGuid = Guid.NewGuid();
            var containerBuilder = new ContainerBuilder();

            if (fileSystem == null)
            {
                fileSystem = new TestDfsFileSystem().FileSystem;
            }

            containerBuilder.RegisterInstance(new PasswordManager(new TestPasswordReader(), new PasswordRegistry())).As<IPasswordManager>().SingleInstance();
            containerBuilder.RegisterInstance(fileSystem).As<IFileSystem>();
            containerBuilder.RegisterType<MigrationManager>().As<IMigrationManager>();
            containerBuilder.RegisterModule<HashingModule>();
            containerBuilder.RegisterInstance(new HashProvider(HashingAlgorithm.GetAlgorithmMetadata(hashName))).As<IHashProvider>();
            containerBuilder.RegisterType<KeyStoreService>().As<IKeyStoreService>().SingleInstance();
            containerBuilder.RegisterModule(new DfsModule());

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
