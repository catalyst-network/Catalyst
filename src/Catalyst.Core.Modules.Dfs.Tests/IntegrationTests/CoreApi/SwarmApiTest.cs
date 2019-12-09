// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using Catalyst.Abstractions.Dfs;
// using Catalyst.Core.Lib.Cryptography;
// using Catalyst.Core.Modules.Dfs.Tests.Utils;
// using Catalyst.Core.Modules.Hashing;
// using Catalyst.TestUtils;
// using Lib.P2P.Cryptography;
// using MultiFormats;
// using MultiFormats.Registry;
// using Newtonsoft.Json.Linq;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
// {
//     public class SwarmApiTest
//     {
//         private IDfsService ipfs;
//
//         public SwarmApiTest(ITestOutputHelper output)
//         {
//             ipfs = new TestFixture(output).Ipfs;      
//         }
//         
//         readonly MultiAddress somewhere = "/ip4/127.0.0.1";
//
//         [Fact]
//         public async Task Filter_Add_Remove()
//         {
//             var addr = await ipfs.Swarm.AddAddressFilterAsync(somewhere);
//             Assert.NotNull(addr);
//             Assert.Equal(somewhere, addr);
//             var addrs = await ipfs.Swarm.ListAddressFiltersAsync();
//             Assert.True(addrs.Any(a => a == somewhere));
//
//             addr = await ipfs.Swarm.RemoveAddressFilterAsync(somewhere);
//             Assert.NotNull(addr);
//             Assert.Equal(somewhere, addr);
//             addrs = await ipfs.Swarm.ListAddressFiltersAsync();
//             Assert.False(addrs.Any(a => a == somewhere));
//         }
//
//         // [Fact]
//         // [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74")]
//         // public async Task Connect_Disconnect_Mars()
//         // {
//         //     var mars = "/dns/mars.i.ipfs.io/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ";
//         //     await ipfs.StartAsync();
//         //     try
//         //     {
//         //         await ipfs.Swarm.ConnectAsync(mars);
//         //         var peers = await ipfs.Swarm.PeersAsync();
//         //         Assert.True(peers.Any(p => p.Id == "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"));
//         //         await ipfs.Swarm.DisconnectAsync(mars);
//         //     }
//         //     finally
//         //     {
//         //         await ipfs.StopAsync();
//         //     }
//         // }
//
//         // [Fact]
//         // [Ignore("TODO: Move to interop tests")]
//         // public async Task JsIPFS_Connect()
//         // {
//         //     var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
//         //     var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
//         //     var remoteAddress = $"/ip4/127.0.0.1/tcp/4002/ipfs/{remoteId}";
//         //
//         //     Assert.Equal(0, (await ipfs.Swarm.PeersAsync()).Count());
//         //     await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
//         //     Assert.Equal(1, (await ipfs.Swarm.PeersAsync()).Count());
//         //
//         //     await ipfs.Swarm.DisconnectAsync(remoteAddress);
//         //     Assert.Equal(0, (await ipfs.Swarm.PeersAsync()).Count());
//         // }
//         //
//         // [Fact]
//         // [Ignore("TODO: Move to interop tests")]
//         // public async Task GoIPFS_Connect()
//         // {
//         //     var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
//         //     var remoteId = "QmdoxrwszT6b9srLXHYBPFVRXmZSFAosWLXoQS9TEEAaix";
//         //     var remoteAddress = $"/ip4/127.0.0.1/tcp/4001/ipfs/{remoteId}";
//         //
//         //     Assert.Equal(0, (await ipfs.Swarm.PeersAsync()).Count());
//         //     await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
//         //     Assert.Equal(1, (await ipfs.Swarm.PeersAsync()).Count());
//         //
//         //     await ipfs.Swarm.DisconnectAsync(remoteAddress);
//         //     Assert.Equal(0, (await ipfs.Swarm.PeersAsync()).Count());
//         // }
//         //
//         // [Fact]
//         // [Ignore("TODO: Move to interop tests")]
//         // public async Task GoIPFS_Connect_v0_4_17()
//         // {
//         //     var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
//         //     var remoteId = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd";
//         //     var remoteAddress = $"/ip4/178.62.158.247/tcp/4001/ipfs/{remoteId}";
//         //
//         //     Assert.Equal(0, (await ipfs.Swarm.PeersAsync()).Count());
//         //     await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
//         //     Assert.Equal(1, (await ipfs.Swarm.PeersAsync()).Count());
//         //
//         //     await ipfs.Swarm.DisconnectAsync(remoteAddress);
//         //     Assert.Equal(0, (await ipfs.Swarm.PeersAsync()).Count());
//         // }
//
//         [Fact]
//         public async Task PrivateNetwork_WithOptionsKey()
//         {
//             using (var ipfs = CreateNode())
//             {
//                 try
//                 {
//                     ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey().Generate();
//                     var swarm = await ipfs.SwarmService;
//                     Assert.NotNull(swarm.NetworkProtector);
//                 }
//                 finally
//                 {
//                     if (Directory.Exists(ipfs.Options.Repository.Folder))
//                     {
//                         Directory.Delete(ipfs.Options.Repository.Folder, true);
//                     }
//                 }
//             }
//         }
//
//         [Fact]
//         public async Task PrivateNetwork_WithSwarmKeyFile()
//         {
//             using (var ipfs = CreateNode())
//             {
//                 try
//                 {
//                     var key = new PreSharedKey().Generate();
//                     var path = Path.Combine(ipfs.Options.Repository.ExistingFolder(), "swarm.key");
//                     using (var x = File.CreateText(path))
//                     {
//                         key.Export(x);
//                     }
//
//                     var swarm = await ipfs.SwarmService;
//                     Assert.NotNull(swarm.NetworkProtector);
//                 }
//                 finally
//                 {
//                     if (Directory.Exists(ipfs.Options.Repository.Folder))
//                     {
//                         Directory.Delete(ipfs.Options.Repository.Folder, true);
//                     }
//                 }
//             }
//         }
//
//         static int nodeNumber = 0;
//
//         DfsService CreateNode()
//         {
//             var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
//             var testPasswordManager = new PasswordManager(new TestPasswordReader(), new PasswordRegistry());
//             var ipfs = new DfsService(hashProvider, testPasswordManager);
//             ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), $"swarm-{nodeNumber++}");
//             ipfs.Options.KeyChain.DefaultKeySize = 512;
//             ipfs.Config.SetAsync(
//                 "Addresses.Swarm",
//                 JToken.FromObject(new string[] {"/ip4/0.0.0.0/tcp/4007"})
//             ).Wait();
//
//             return ipfs;
//         }
//     }
// }
