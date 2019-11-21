using System.IO;
using Common.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Ipfs.Core.Tests
{
    [TestClass]
    public class TestFixture
    {
        const string Passphrase = "this is not a secure pass phrase";
        public static IpfsEngine Ipfs = new IpfsEngine(Passphrase.ToCharArray());
        public static IpfsEngine IpfsOther = new IpfsEngine(Passphrase.ToCharArray());

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

        [TestMethod]
        public void Engine_Exists()
        {
            Assert.IsNotNull(Ipfs);
            Assert.IsNotNull(IpfsOther);
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            // set logger factory
            var properties = new Common.Logging.Configuration.NameValueCollection
            {
                ["level"] = "DEBUG",
                ["showLogName"] = "true",
                ["showDateTime"] = "true",
                ["dateTimeFormat"] = "HH:mm:ss.fff"
            };
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            if (Directory.Exists(Ipfs.Options.Repository.Folder))
            {
                Directory.Delete(Ipfs.Options.Repository.Folder, true);
            }

            if (Directory.Exists(IpfsOther.Options.Repository.Folder))
            {
                Directory.Delete(IpfsOther.Options.Repository.Folder, true);
            }
        }
    }
}
