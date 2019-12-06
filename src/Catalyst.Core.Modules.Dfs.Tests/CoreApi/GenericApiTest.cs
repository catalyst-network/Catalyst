using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper.Configuration.Annotations;
using Lib.P2P;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class GenericApiTest
    {
        [Fact]
        public async Task Local_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var peer = await ipfs.Generic.IdAsync();
            Assert.IsType(typeof(Peer), peer);
            Assert.NotNull(peer.Addresses);
            Assert.StartsWith(peer.AgentVersion, "net-ipfs/");
            Assert.NotNull(peer.Id);
            Assert.StartsWith(peer.ProtocolVersion, "ipfs/");
            Assert.NotNull(peer.PublicKey);

            Assert.True(peer.IsValid());
        }

        [Fact]
        public async Task Mars_Info()
        {
            var marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
            var marsAddr = $"/ip6/::1/p2p/{marsId}";
            var ipfs = TestFixture.Ipfs;
            var swarm = await ipfs.SwarmService;
            var mars = swarm.RegisterPeerAddress(marsAddr);

            var peer = await ipfs.Generic.IdAsync(marsId);
            Assert.Equal(mars.Id, peer.Id);
            Assert.Equal(mars.Addresses.First(), peer.Addresses.First());
        }

        [Fact]
        public async Task Version_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var versions = await ipfs.Generic.VersionAsync();
            Assert.NotNull(versions);
            Assert.True(versions.ContainsKey("Version"));
            Assert.True(versions.ContainsKey("Repo"));
        }

        [Fact]
        public async Task Shutdown()
        {
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            await ipfs.Generic.ShutdownAsync();
        }

        [Fact]
        public async Task Resolve_Cid()
        {
            var ipfs = TestFixture.Ipfs;
            var actual = await ipfs.Generic.ResolveAsync("QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.Equal("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);

            actual = await ipfs.Generic.ResolveAsync("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.Equal("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);
        }

        [Fact]
        public async Task Resolve_Cid_Path()
        {
            var ipfs = TestFixture.Ipfs;
            var temp = FileSystemApiTest.MakeTemp();
            try
            {
                var dir = await ipfs.FileSystem.AddDirectoryAsync(temp, true);
                var name = "/ipfs/" + dir.Id.Encode() + "/x/y/y.txt";
                Assert.Equal("/ipfs/QmTwEE2eSyzcvUctxP2negypGDtj7DQDKVy8s3Rvp6y6Pc",
                    await ipfs.Generic.ResolveAsync(name));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public void Resolve_Cid_Invalid()
        {
            var ipfs = TestFixture.Ipfs;
            ExceptionAssert.Throws<FormatException>(() =>
            {
                var _ = ipfs.Generic.ResolveAsync("QmHash").Result;
            });
        }

        [Fact]
        public async Task Resolve_DnsLink()
        {
            var ipfs = TestFixture.Ipfs;
            var path = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io");
            Assert.NotNull(path);
        }

        // [Fact]
        // [Ignore("Need a working IPNS")]
        // public async Task Resolve_DnsLink_Recursive()
        // {
        //     var ipfs = TestFixture.Ipfs;
        //
        //     var media = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media");
        //     var actual = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media", recursive: true);
        //     Assert.NotEqual(media, actual);
        // }
    }
}
