using System;
using System.IO;
using Autofac;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Dfs.Migration;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Modules.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.Migration;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using FluentAssertions;
using Lib.P2P.Routing;
using Microsoft.AspNetCore.Mvc.Internal;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.Utils
{
    public class TestDfs
    {
        private sealed class TestDfsFileSystem : FileSystemBasedTest
        {
            internal TestDfsFileSystem(ITestOutputHelper output) : base(output) { }
        }
        
        public static IDfsService GetTestDfs(ITestOutputHelper output, string folderName = default, string keyType = default)
        {
            var nodeGuid = Guid.NewGuid();
            var containerBuilder = new ContainerBuilder();
            var filesystem = new TestDfsFileSystem(output);

            if (string.Equals(folderName, default, StringComparison.Ordinal))
            {
                folderName = nodeGuid.ToString();
            }

            if (keyType == default)
            {
                keyType = "rsa";

                //"ed25519";
            }

            containerBuilder.RegisterInstance(new PasswordManager(new TestPasswordReader(), new PasswordRegistry())).As<IPasswordManager>().SingleInstance();
            containerBuilder.RegisterInstance(filesystem.FileSystem).As<IFileSystem>();
            containerBuilder.RegisterType<MigrationManager>().As<IMigrationManager>();
            containerBuilder.RegisterModule<HashingModule>();
            //containerBuilder.RegisterType<KatDhtService>().As<IDhtService>().SingleInstance();
            //containerBuilder.RegisterType<DhtApi>().As<IDhtApi>().SingleInstance();
            containerBuilder.RegisterInstance(new KeyStoreService(filesystem.FileSystem)).As<IKeyStoreService>().SingleInstance();
            containerBuilder.RegisterModule(new DfsModule());

            var container = containerBuilder.Build();
            var scope = container.BeginLifetimeScope(nodeGuid);
            var dfsService = scope.Resolve<IDfsService>();
            
            dfsService.Options.Repository.Folder = Path.Combine(filesystem.FileSystem.GetCatalystDataDir().FullName, folderName);
            dfsService.Options.KeyChain.DefaultKeySize = 512;
            dfsService.Options.KeyChain.DefaultKeyType = keyType;

            dfsService.ConfigApi.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { "/ip4/0.0.0.0/tcp/0" })
            ).Wait();

            return dfsService;
        }
    }
}
