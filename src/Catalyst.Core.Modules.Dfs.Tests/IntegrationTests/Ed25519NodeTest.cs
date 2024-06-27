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

// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using Catalyst.Abstractions.Dfs;
// using Catalyst.Core.Lib.Cryptography;
// using Catalyst.Core.Modules.Dfs.Tests.Utils;
// using Catalyst.Core.Modules.Hashing;
// using Catalyst.TestUtils;
// using MultiFormats.Registry;
// using Newtonsoft.Json.Linq;
// using NUnit.Framework;
// 
//
// namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
// {
//     public class Ed25519NodeTest
//     {
//         private IDfsService ipfs;
//
//         public Ed25519NodeTest(TestContext output)
//         {
//             ipfs = new TestFixture(output).Ipfs;      
//         }
//         
//         [Test]
//         public async Task Can_Create()
//         {
//             var ed = await CreateNode();
//             try
//             {
//                 Assert.NotNull(ed);
//                 var node = await ed.LocalPeer;
//                 Assert.NotNull(node);
//             }
//             finally
//             {
//                 DeleteNode(ed);
//             }
//         }
//
//         [Test]
//         public async Task CanConnect()
//         {
//             var ed = await CreateNode();
//             try
//             {
//                 await ed.StartAsync();
//                 var node = await ed.LocalPeer;
//                 Assert.AreNotEqual(0, node.Addresses.Count());
//                 var addr = node.Addresses.First();
//                 await ipfs.StartAsync();
//                 try
//                 {
//                     await ipfs.Swarm.ConnectAsync(addr);
//                     var peers = await ipfs.Swarm.PeersAsync();
//                     Assert.True(peers.Any(p => p.Id == addr.Address));
//                     await ipfs.Swarm.DisconnectAsync(addr);
//                 }
//                 finally
//                 {
//                     await ipfs.StopAsync();
//                 }
//             }
//             finally
//             {
//                 await ed.StopAsync();
//                 DeleteNode(ed);
//             }
//         }
//
//         async Task<DfsService> CreateNode()
//         {
//             var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
//             var testPasswordManager = new PasswordManager(new TestPasswordReader(), new PasswordRegistry());
//             var ipfs = new DfsService(hashProvider, testPasswordManager);
//             ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-ed255129-test");
//             ipfs.Options.KeyChain.DefaultKeyType = "ed25519";
//             await ipfs.Config.SetAsync(
//                 "Addresses.Swarm",
//                 JToken.FromObject(new string[] {"/ip4/0.0.0.0/tcp/0"})
//             );
//             return ipfs;
//         }
//
//         void DeleteNode(DfsService ipfs)
//         {
//             if (Directory.Exists(ipfs.Options.Repository.Folder))
//             {
//                 Directory.Delete(ipfs.Options.Repository.Folder, true);
//             }
//         }
//     }
// }
