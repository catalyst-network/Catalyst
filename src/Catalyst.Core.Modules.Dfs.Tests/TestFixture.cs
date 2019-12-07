using System.Collections.Generic;
using System.IO;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using Common.Logging;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MultiFormats.Registry;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    public class TestFixture
    {
        public IDfs Ipfs;
        public IDfs IpfsOther;

        private class TestDfsFileSystem : FileSystemBasedTest
        {
            protected internal TestDfsFileSystem(ITestOutputHelper output) : base(output) { }
        }

        public TestFixture(ITestOutputHelper output)
        {
            var filesystem = new TestDfsFileSystem(output);
            
            Ipfs = new Dfs(new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("sha2-256")), new PasswordManager(new TestPasswordReader(), new PasswordRegistry()));
            Ipfs.Options.Repository.Folder = Path.Combine(filesystem.FileSystem.GetCatalystDataDir().FullName, "ipfs-test");
            Ipfs.Options.KeyChain.DefaultKeySize = 512;
            Ipfs.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] {"/ip4/0.0.0.0/tcp/0"})
            ).Wait();
            
            IpfsOther = new Dfs(new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("sha2-256")), new PasswordManager(new TestPasswordReader(), new PasswordRegistry()));

            IpfsOther.Options.Repository.Folder = Path.Combine(filesystem.FileSystem.GetCatalystDataDir().FullName, "ipfs-other");
            IpfsOther.Options.KeyChain.DefaultKeySize = 512;
            IpfsOther.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] {"/ip4/0.0.0.0/tcp/0"})
            ).Wait();
        }

        [Fact]
        public void Engine_Exists()
        {
            Assert.NotNull(Ipfs);
            Assert.NotNull(IpfsOther);
        }

        // [AssemblyInitialize]
        // public static void AssemblyInitialize(TestContext context)
        // {
        //     // set logger factory
        //     var properties = new Common.Logging.Configuration.NameValueCollection
        //     {
        //         ["level"] = "DEBUG",
        //         ["showLogName"] = "true",
        //         ["showDateTime"] = "true",
        //         ["dateTimeFormat"] = "HH:mm:ss.fff"
        //     };
        //     LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);
        // }
        //
        // [AssemblyCleanup]
        // public static void Cleanup()
        // {
        //     if (Directory.Exists(Ipfs.Options.Repository.Folder))
        //     {
        //         Directory.Delete(Ipfs.Options.Repository.Folder, true);
        //     }
        //
        //     if (Directory.Exists(IpfsOther.Options.Repository.Folder))
        //     {
        //         Directory.Delete(IpfsOther.Options.Repository.Folder, true);
        //     }
        // }
    }
}
