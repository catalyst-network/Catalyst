using System.IO;
using Catalyst.Abstractions.Dfs;
using Common.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    public class TestFixture
    {
        const string passphrase = "this is not a secure pass phrase";
        public static IDfs Ipfs = new Dfs();
        public static IDfs IpfsOther = new Dfs();

        static TestFixture()
        {
            Ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-test");
            Ipfs.Options.KeyChain.DefaultKeySize = 512;
            Ipfs.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] {"/ip4/0.0.0.0/tcp/0"})
            ).Wait();

            IpfsOther.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-other");
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
