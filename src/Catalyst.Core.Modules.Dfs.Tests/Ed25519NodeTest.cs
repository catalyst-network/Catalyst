using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    [TestClass]
    public class Ed25519NodeTest
    {
        [Fact]
        public async Task Can_Create()
        {
            var ed = await CreateNode();
            try
            {
                Assert.NotNull(ed);
                var node = await ed.LocalPeer;
                Assert.NotNull(node);
            }
            finally
            {
                DeleteNode(ed);
            }
        }

        [Fact]
        public async Task CanConnect()
        {
            var ed = await CreateNode();
            try
            {
                await ed.StartAsync();
                var node = await ed.LocalPeer;
                Assert.NotEqual(0, node.Addresses.Count());
                var addr = node.Addresses.First();

                var ipfs = TestFixture.Ipfs;
                await ipfs.StartAsync();
                try
                {
                    await ipfs.Swarm.ConnectAsync(addr);
                    var peers = await ipfs.Swarm.PeersAsync();
                    Assert.True(peers.Any(p => p.Id == addr.PeerId));
                    await ipfs.Swarm.DisconnectAsync(addr);
                }
                finally
                {
                    await ipfs.StopAsync();
                }
            }
            finally
            {
                await ed.StopAsync();
                DeleteNode(ed);
            }
        }

        async Task<Dfs> CreateNode()
        {
            const string passphrase = "this is not a secure pass phrase";
            var ipfs = new Dfs(passphrase.ToCharArray());
            ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-ed255129-test");
            ipfs.Options.KeyChain.DefaultKeyType = "ed25519";
            await ipfs.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] {"/ip4/0.0.0.0/tcp/0"})
            );
            return ipfs;
        }

        void DeleteNode(Dfs ipfs)
        {
            if (Directory.Exists(ipfs.Options.Repository.Folder))
            {
                Directory.Delete(ipfs.Options.Repository.Folder, true);
            }
        }
    }
}
