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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.Core.Modules.Dfs.UnixFs;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Tar;
using Lib.P2P;
using Lib.P2P.Cryptography;
using MultiFormats;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class FileSystemApiTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private IDfsService ipfs;

        public FileSystemApiTest(ITestOutputHelper output)
        {
            _testOutputHelper = output;
            ipfs = TestDfs.GetTestDfs(output, null, "sha2-256");
        }
        
        [Fact]
        public async Task AddText()
        {
            var node = (UnixFsNode) await ipfs.UnixFsApi.AddTextAsync("hello world");
            Assert.Equal("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", node.Id);
            Assert.Equal("", node.Name);
            Assert.Equal(0, node.Links.Count());

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("hello world", text);

            var actual = await ipfs.UnixFsApi.ListFileAsync(node.Id);
            Assert.Equal(node.Id, actual.Id);
            Assert.Equal(node.IsDirectory, actual.IsDirectory);
            Assert.Equal(node.Links.Count(), actual.Links.Count());
            Assert.Equal(node.Size, actual.Size);
        }

        [Fact]
        public async Task AddEmptyText()
        {
            var node = (UnixFsNode) await ipfs.UnixFsApi.AddTextAsync("");
            Assert.Equal("QmbFMke1KXqnYyBBWxB74N4c5SBnJMVAiMNRcGu6x1AwQH", node.Id);
            Assert.Equal("", node.Name);
            Assert.Equal(0, node.Links.Count());

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("", text);

            var actual = await ipfs.UnixFsApi.ListFileAsync(node.Id);
            Assert.Equal(node.Id, actual.Id);
            Assert.Equal(node.IsDirectory, actual.IsDirectory);
            Assert.Equal(node.Links.Count(), actual.Links.Count());
            Assert.Equal(node.Size, actual.Size);
        }

        [Fact]
        public async Task AddEmpty_Check_Object()
        {
            // see https://github.com/ipfs/js-ipfs-unixfs/pull/25
            var node = await ipfs.UnixFsApi.AddTextAsync("");
            var block = await ipfs.ObjectApi.GetAsync(node.Id);
            var expected = new byte[] {0x08, 0x02, 0x18, 0x00};
            Assert.Equal(node.Id, block.Id);
            Assert.Equal(expected, block.DataBytes);
        }

        [Fact]
        public async Task AddDuplicateWithPin()
        {
            var options = new AddFileOptions
            {
                Pin = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.Equal("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", node.Id);
            var pins = await ipfs.PinApi.ListAsync();
            pins.ToArray().Should().Contain(node.Id);

            options.Pin = false;
            node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.Equal("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", node.Id);
            Assert.Equal(0, node.Links.Count());
            pins = await ipfs.PinApi.ListAsync();
            pins.ToArray().Should().NotContain(node.Id);
        }

        [Fact]
        public async Task Add_SizeChunking()
        {
            var options = new AddFileOptions {ChunkSize = 3, Pin = true};
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.Equal("QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn", node.Id);
            Assert.Equal(false, node.IsDirectory);
            Assert.Equal(4, links.Length);
            Assert.Equal("QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6", links[0].Id);
            Assert.Equal("QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN", links[1].Id);
            Assert.Equal("QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6", links[2].Id);
            Assert.Equal("QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE", links[3].Id);

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("hello world", text);
        }

        [Fact]
        public async Task StreamBehaviour()
        {
            var options = new AddFileOptions
            {
                ChunkSize = 3,
                Pin = true,
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var stream = await ipfs.UnixFsApi.ReadFileAsync(node.Id);
            Assert.Equal(11, stream.Length);
            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
            Assert.True(stream.CanSeek);
        }

        [Fact]
        public async Task Add_HashAlgorithm()
        {
            var options = new AddFileOptions
            {
                Hash = "blake2b-256",
                RawLeaves = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.Equal("bafk2bzaceaswza5ss4iu2ia3galz6pyo6dfm5f4dmiw2lf2de22dmf4k533ba", node.Id);

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("hello world", text);
        }

        [Fact]
        public void AddFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var node = (UnixFsNode) ipfs.UnixFsApi.AddFileAsync(path).Result;
                Assert.Equal("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", node.Id);
                Assert.Equal(0, node.Links.Count());
                Assert.Equal(Path.GetFileName(path), node.Name);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AddFile_CidEncoding()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var options = new AddFileOptions
                {
                    Encoding = "base32"
                };
                var node = ipfs.UnixFsApi.AddFileAsync(path, options).Result;
                Assert.Equal("base32", node.Id.Encoding);
                Assert.Equal(1, node.Id.Version);
                Assert.Equal(0, node.Links.Count());

                var text = ipfs.UnixFsApi.ReadAllTextAsync(node.Id).Result;
                Assert.Equal("hello world", text);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AddFile_Large()
        {
            AddFile(); // warm up

            var path = "star_trails.mp4";
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var node = ipfs.UnixFsApi.AddFileAsync(path).Result;
            stopWatch.Stop();
            
            // _testOutputHelper.WriteLine("Add file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);

            Assert.Equal("QmeZkAUfUFPq5YWGBan2ZYNd9k59DD1xW62pGJrU3C6JRo", node.Id);

            var k = 8 * 1024;
            var buffer1 = new byte[k];
            var buffer2 = new byte[k];
            stopWatch.Restart();
            using (var localStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using var ipfsStream = ipfs.UnixFsApi.ReadFileAsync(node.Id).Result;
                while (true)
                {
                    var n1 = localStream.Read(buffer1, 0, k);
                    var n2 = ipfsStream.Read(buffer2, 0, k);
                    Assert.Equal(n1, n2);
                    if (n1 == 0)
                    {
                        break;
                    }
                    
                    for (var i = 0; i < n1; ++i)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            throw new XunitException("data not the same");
                        }
                    }
                }
            }

            stopWatch.Stop();
            
            // _testOutputHelper.WriteLine("Readfile file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);
        }

        /// <seealso href="https://github.com/richardschneider/net-ipfs-engine/issues/125"/>
        [Fact]
        public void AddFile_Larger()
        {
            AddFile(); // warm up

            var path = "starx2.mp4";
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var node = ipfs.UnixFsApi.AddFileAsync(path).Result;
            stopWatch.Stop();
            _testOutputHelper.WriteLine("Add file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);

            Assert.Equal("QmeFhfB4g2GFbxYb7usApWzq8uC1vmuxJajFpiJiT5zLoy", node.Id);

            const int k = 8 * 1024;
            var buffer1 = new byte[k];
            var buffer2 = new byte[k];
            stopWatch.Restart();
            using (var localStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using var ipfsStream = ipfs.UnixFsApi.ReadFileAsync(node.Id).Result;
                while (true)
                {
                    var n1 = localStream.Read(buffer1, 0, k);
                    var n2 = ipfsStream.Read(buffer2, 0, k);
                    Assert.Equal(n1, n2);
                    if (n1 == 0)
                    {
                        break;
                    }
                    
                    for (var i = 0; i < n1; ++i)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            throw new XunitException("data not the same");
                        }
                    }
                }
            }

            stopWatch.Stop();
            _testOutputHelper.WriteLine("Readfile file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);
        }

        [Fact]
        public async Task AddFile_Wrap()
        {
            const string path = "hello.txt";
            File.WriteAllText(path, "hello world");
            try
            {
                var options = new AddFileOptions
                {
                    Wrap = true
                };
                var node = await ipfs.UnixFsApi.AddFileAsync(path, options);
                Assert.Equal("QmNxvA5bwvPGgMXbmtyhxA1cKFdvQXnsGnZLCGor3AzYxJ", node.Id);
                Assert.Equal(true, node.IsDirectory);
                Assert.Equal(1, node.Links.Count());
                Assert.Equal("hello.txt", node.Links.First().Name);
                Assert.Equal("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", node.Links.First().Id);
                Assert.Equal(19, node.Links.First().Size);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task Add_Raw()
        {
            var options = new AddFileOptions
            {
                RawLeaves = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.Equal("bafkreifzjut3te2nhyekklss27nh3k72ysco7y32koao5eei66wof36n5e", node.Id);
            Assert.Equal(11, node.Size);
            Assert.Equal(0, node.Links.Count());
            Assert.Equal(false, node.IsDirectory);

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("hello world", text);
        }

        [Fact]
        public async Task Add_Inline()
        {
            var original = ipfs.Options.Block.AllowInlineCid;
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;

                var node = await ipfs.UnixFsApi.AddTextAsync("hiya");
                Assert.Equal(1, node.Id.Version);
                Assert.True(node.Id.Hash.IsIdentityHash);
                Assert.Equal(4, node.Size);
                Assert.Equal(0, node.Links.Count());
                Assert.Equal(false, node.IsDirectory);
                Assert.Equal("bafyaadakbieaeeqenbuxsyiyaq", node.Id.Encode());
                var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
                Assert.Equal("hiya", text);
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = original;
            }
        }

        [Fact]
        public async Task Add_RawAndChunked()
        {
            var options = new AddFileOptions
            {
                RawLeaves = true,
                ChunkSize = 3
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.Equal("QmUuooB6zEhMmMaBvMhsMaUzar5gs5KwtVSFqG4C1Qhyhs", node.Id);
            Assert.Equal(false, node.IsDirectory);
            Assert.Equal(4, links.Length);
            Assert.Equal("bafkreigwvapses57f56cfow5xvoua4yowigpwcz5otqqzk3bpcbbjswowe", links[0].Id);
            Assert.Equal("bafkreiew3cvfrp2ijn4qokcp5fqtoknnmr6azhzxovn6b3ruguhoubkm54", links[1].Id);
            Assert.Equal("bafkreibsybcn72tquh2l5zpim2bba4d2kfwcbpzuspdyv2breaq5efo7tq", links[2].Id);
            Assert.Equal("bafkreihfuch72plvbhdg46lef3n5zwhnrcjgtjywjryyv7ffieyedccchu", links[3].Id);

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("hello world", text);
        }

        [Fact]
        public async Task Add_Protected()
        {
            var options = new AddFileOptions
            {
                ProtectionKey = "self"
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.Equal("cms", node.Id.ContentType);
            Assert.Equal(0, node.Links.Count());
            Assert.Equal(false, node.IsDirectory);

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("hello world", text);
        }

        [Fact]
        public async Task Add_Protected_Chunked()
        {
            var options = new AddFileOptions
            {
                ProtectionKey = "self",
                ChunkSize = 3
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.Equal(4, node.Links.Count());
            Assert.Equal(false, node.IsDirectory);

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.Equal("hello world", text);
        }

        [Fact]
        public async Task Add_OnlyHash()
        {
            var nodes = new[]
            {
                "QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn",
                "QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6",
                "QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN",
                "QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6",
                "QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE"
            };
            foreach (var n in nodes)
            {
                await ipfs.BlockApi.RemoveAsync(n, ignoreNonexistent: true);
            }

            var options = new AddFileOptions
            {
                ChunkSize = 3,
                OnlyHash = true,
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.Equal(nodes[0], node.Id);
            Assert.Equal(nodes.Length - 1, links.Length);
            for (var i = 0; i < links.Length; ++i)
            {
                Assert.Equal(nodes[i + 1], links[i].Id);
            }

            // TODO: Need a method to test that the CId is not held locally.
            //foreach (var n in nodes)
            //{
            //    Assert.Null(await ipfs.Block.StatAsync(n));
            //}
        }

        [Fact]
        public async Task ReadWithOffset()
        {
            const string text = "hello world";
            var options = new AddFileOptions
            {
                ChunkSize = 3
            };
            var node = await ipfs.UnixFsApi.AddTextAsync(text, options);

            for (var offset = 0; offset <= text.Length; ++offset)
            {
                await using (var data = await ipfs.UnixFsApi.ReadFileAsync(node.Id, offset))
                {
                    using var reader = new StreamReader(data);
                    {
                        var readData = reader.ReadToEnd();
                        Assert.Equal(text.Substring(offset), readData);
                    }
                }
            }
        }

        [Fact]
        public async Task Read_RawWithLength()
        {
            const string text = "hello world";
            var options = new AddFileOptions
            {
                RawLeaves = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync(text, options);

            for (var offset = 0; offset < text.Length; ++offset)
            {
                for (var length = text.Length + 1; 0 < length; --length)
                {
                    await using (var data = await ipfs.UnixFsApi.ReadFileAsync(node.Id, offset, length))
                    {
                        using var reader = new StreamReader(data);
                        {
                            var readData = reader.ReadToEnd();
                            Assert.Equal(text.Substring(offset, Math.Min(11 - offset, length)), readData);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Read_ChunkedWithLength()
        {
            const string text = "hello world";
            var options = new AddFileOptions
            {
                ChunkSize = 3
            };
            var node = await ipfs.UnixFsApi.AddTextAsync(text, options);

            for (var length = text.Length + 1; 0 < length; --length)
            {
                await using (var data = await ipfs.UnixFsApi.ReadFileAsync(node.Id, 0, length))
                {
                    using var reader = new StreamReader(data);
                    {
                        var readData = reader.ReadToEnd();
                        Assert.Equal(text.Substring(0, Math.Min(11, length)), readData);
                    }
                }
            }
        }

        [Fact]
        public async Task Read_ProtectedWithLength()
        {
            const string text = "hello world";
            var options = new AddFileOptions
            {
                ProtectionKey = "self"
            };
            var node = await ipfs.UnixFsApi.AddTextAsync(text, options);

            for (var offset = 0; offset < text.Length; ++offset)
            {
                for (var length = text.Length + 1; 0 < length; --length)
                {
                    await using (var data = await ipfs.UnixFsApi.ReadFileAsync(node.Id, offset, length))
                    {
                        using (var reader = new StreamReader(data))
                        {
                            var readData = reader.ReadToEnd();
                            Assert.Equal(text.Substring(offset, Math.Min(11 - offset, length)), readData);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Read_ProtectedChunkedWithLength()
        {
            const string text = "hello world";
            var options = new AddFileOptions
            {
                ChunkSize = 3,
                ProtectionKey = "self"
            };
            var node = await ipfs.UnixFsApi.AddTextAsync(text, options);

            for (var offset = 0; offset < text.Length; ++offset)
            {
                for (var length = text.Length + 1; 0 < length; --length)
                {
                    await using (var data = await ipfs.UnixFsApi.ReadFileAsync(node.Id, offset, length))
                    {
                        using (var reader = new StreamReader(data))
                        {
                            var readData = reader.ReadToEnd();
                            Assert.Equal(text.Substring(offset, Math.Min(11 - offset, length)), readData);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Read_ProtectedMissingKey()
        {
            const string text = "hello world";
            var key = await ipfs.KeyApi.CreateAsync("alice", "rsa", 512);
            try
            {
                var options = new AddFileOptions {ProtectionKey = key.Name};
                var node = await ipfs.UnixFsApi.AddTextAsync(text, options);
                Assert.Equal(text, await ipfs.UnixFsApi.ReadAllTextAsync(node.Id));

                await ipfs.KeyApi.RemoveAsync(key.Name);
                ExceptionAssert.Throws<KeyNotFoundException>(() =>
                {
                    var _ = ipfs.UnixFsApi.ReadAllTextAsync(node.Id).Result;
                });
            }
            finally
            {
                await ipfs.KeyApi.RemoveAsync(key.Name);
            }
        }

        [Fact]
        public async Task AddFile_WithProgress()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                TransferProgress lastProgress = null;
                var options = new AddFileOptions
                {
                    ChunkSize = 3,
                    Progress = new Progress<TransferProgress>(t => { lastProgress = t; })
                };
                
                await ipfs.UnixFsApi.AddFileAsync(path, options);

                // Progress reports get posted on another synchronisation context
                // so they can come in later.
                var stop = DateTime.Now.AddSeconds(3);
                while (DateTime.Now < stop && lastProgress?.Bytes == 11UL)
                {
                    await Task.Delay(10);
                }

                Assert.Equal(11UL, lastProgress.Bytes);
                Assert.Equal(Path.GetFileName(path), lastProgress.Name);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AddDirectory()
        {
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp, false).Result;
                Assert.True(dir.IsDirectory);

                var files = dir.Links.ToArray();
                Assert.Equal(2, files.Length);
                Assert.Equal("alpha.txt", files[0].Name);
                Assert.Equal("beta.txt", files[1].Name);

                Assert.Equal("alpha", ipfs.UnixFsApi.ReadAllTextAsync(files[0].Id).Result);
                Assert.Equal("beta", ipfs.UnixFsApi.ReadAllTextAsync(files[1].Id).Result);

                Assert.Equal("alpha", ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/alpha.txt").Result);
                Assert.Equal("beta", ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/beta.txt").Result);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public void AddDirectoryRecursive()
        {
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp).Result;
                Assert.True(dir.IsDirectory);

                var files = dir.Links.ToArray();
                Assert.Equal(3, files.Length);
                Assert.Equal("alpha.txt", files[0].Name);
                Assert.Equal("beta.txt", files[1].Name);
                Assert.Equal("x", files[2].Name);
                Assert.NotEqual(0, files[0].Size);
                Assert.NotEqual(0, files[1].Size);

                var rootFiles = ipfs.UnixFsApi.ListFileAsync(dir.Id).Result.Links.ToArray();
                Assert.Equal(3, rootFiles.Length);
                Assert.Equal("alpha.txt", rootFiles[0].Name);
                Assert.Equal("beta.txt", rootFiles[1].Name);
                Assert.Equal("x", rootFiles[2].Name);

                var xfiles = ipfs.UnixFsApi.ListFileAsync(rootFiles[2].Id).Result.Links.ToArray();
                Assert.Equal(2, xfiles.Length);
                Assert.Equal("x.txt", xfiles[0].Name);
                Assert.Equal("y", xfiles[1].Name);

                var yfiles = ipfs.UnixFsApi.ListFileAsync(xfiles[1].Id).Result.Links.ToArray();
                Assert.Equal(1, yfiles.Length);
                Assert.Equal("y.txt", yfiles[0].Name);

                Assert.Equal("x", ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/x/x.txt").Result);
                Assert.Equal("y", ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/x/y/y.txt").Result);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public void AddDirectory_WithHashAlgorithm()
        {
            const string alg = "keccak-512";
            var options = new AddFileOptions {Hash = alg};
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp, false, options).Result;
                Assert.True(dir.IsDirectory);
                Assert.Equal(alg, dir.Id.Hash.Algorithm.Name);

                foreach (var link in dir.Links)
                {
                    Assert.Equal(alg, link.Id.Hash.Algorithm.Name);
                }
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public void AddDirectory_WithCidEncoding()
        {
            var encoding = "base32z";
            var options = new AddFileOptions {Encoding = encoding};
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp, false, options).Result;
                Assert.True(dir.IsDirectory);
                Assert.Equal(encoding, dir.Id.Encoding);

                foreach (var link in dir.Links)
                {
                    Assert.Equal(encoding, link.Id.Encoding);
                }
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public async Task AddDirectoryRecursive_ObjectLinks()
        {
            var temp = MakeTemp();
            try
            {
                var dir = await ipfs.UnixFsApi.AddDirectoryAsync(temp);
                Assert.True(dir.IsDirectory);

                var cid = dir.Id;
                var i = 0;
                var allLinks = new List<IMerkleLink>();
                while (cid != null)
                {
                    var links = await ipfs.ObjectApi.LinksAsync(cid);
                    allLinks.AddRange(links);
                    cid = i < allLinks.Count ? allLinks[i++].Id : null;
                }

                Assert.Equal(6, allLinks.Count);
                Assert.Equal("alpha.txt", allLinks[0].Name);
                Assert.Equal("beta.txt", allLinks[1].Name);
                Assert.Equal("x", allLinks[2].Name);
                Assert.Equal("x.txt", allLinks[3].Name);
                Assert.Equal("y", allLinks[4].Name);
                Assert.Equal("y.txt", allLinks[5].Name);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        // [Fact]
        // [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74")]
        // public async Task ReadTextFromNetwork()
        // {
        //     var ipfs = TestFixture.Ipfs;
        //     await ipfs.StartAsync();
        //
        //     try
        //     {
        //         var folder = "QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv";
        //         await ipfs.Block.RemoveAsync(folder, true);
        //
        //         var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        //         var text = await ipfs.FileSystem.ReadAllTextAsync($"{folder}/about", cts.Token);
        //         Assert.Contains(text, "IPFS -- Inter-Planetary File system");
        //     }
        //     finally
        //     {
        //         await ipfs.StopAsync();
        //     }
        // }

        [Fact]
        public async Task Read_From_OtherNode()
        {
            using (var a = TestDfs.GetTestDfs(_testOutputHelper))
            {
                using (var b = TestDfs.GetTestDfs(_testOutputHelper))
                {
                    using (var c = TestDfs.GetTestDfs(_testOutputHelper))
                    {
                        var psk = new PreSharedKey().Generate();

                        // Start bootstrap node.
                        b.Options.Discovery.DisableMdns = true;
                        b.Options.Swarm.MinConnections = 0;
                        b.Options.Swarm.PrivateNetworkKey = psk;
                        b.Options.Discovery.BootstrapPeers = new MultiAddress[0];
                        await b.StartAsync();
                        var bootstrapPeers = new[]
                        {
                            b.LocalPeer.Addresses.First()
                        };
                        _testOutputHelper.WriteLine($"B is {b.LocalPeer}");

                        // Node that has the content.
                        c.Options.Discovery.DisableMdns = true;
                        c.Options.Swarm.MinConnections = 0;
                        c.Options.Swarm.PrivateNetworkKey = psk;
                        c.Options.Discovery.BootstrapPeers = bootstrapPeers;
                        await c.StartAsync();
                        await c.SwarmApi.ConnectAsync(bootstrapPeers[0]);
                        _testOutputHelper.WriteLine($"C is {c.LocalPeer}");

                        var fsn = await c.UnixFsApi.AddTextAsync("some content");
                        var cid = fsn.Id;

                        // Node that reads the content.
                        a.Options.Discovery.DisableMdns = true;
                        a.Options.Swarm.MinConnections = 0;
                        a.Options.Swarm.PrivateNetworkKey = psk;
                        a.Options.Discovery.BootstrapPeers = bootstrapPeers;
                        await a.StartAsync();
                        _testOutputHelper.WriteLine($"A is {a.LocalPeer}");
                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var content = await a.UnixFsApi.ReadAllTextAsync(cid, cts.Token);
                        Assert.Equal("some content", content);
                    }
                }
            }
        }

        [Fact]
        public async Task GetTar()
        {
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp).Result;
                var dirid = dir.Id.Encode();

                var tar = await ipfs.UnixFsApi.GetAsync(dir.Id);
                var archive = TarArchive.CreateInputTarArchive(tar);
                var files = new List<string>();
                archive.ProgressMessageEvent += (a, e, m) => { files.Add(e.Name); };
                archive.ListContents();

                Assert.Equal($"{dirid}", files[0]);
                Assert.Equal($"{dirid}/alpha.txt", files[1]);
                Assert.Equal($"{dirid}/beta.txt", files[2]);
                Assert.Equal($"{dirid}/x", files[3]);
                Assert.Equal($"{dirid}/x/x.txt", files[4]);
                Assert.Equal($"{dirid}/x/y", files[5]);
                Assert.Equal($"{dirid}/x/y/y.txt", files[6]);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public async Task GetTar_RawLeaves()
        {
            var temp = MakeTemp();
            try
            {
                var options = new AddFileOptions
                {
                    RawLeaves = true
                };
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp, true, options).Result;
                var dirid = dir.Id.Encode();

                var tar = await ipfs.UnixFsApi.GetAsync(dir.Id);
                var archive = TarArchive.CreateInputTarArchive(tar);
                var files = new List<string>();
                archive.ProgressMessageEvent += (a, e, m) => { files.Add(e.Name); };
                archive.ListContents();

                Assert.Equal($"{dirid}", files[0]);
                Assert.Equal($"{dirid}/alpha.txt", files[1]);
                Assert.Equal($"{dirid}/beta.txt", files[2]);
                Assert.Equal($"{dirid}/x", files[3]);
                Assert.Equal($"{dirid}/x/x.txt", files[4]);
                Assert.Equal($"{dirid}/x/y", files[5]);
                Assert.Equal($"{dirid}/x/y/y.txt", files[6]);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public async Task GetTar_EmptyDirectory()
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(temp);
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp).Result;
                var _ = dir.Id.Encode();

                var tar = await ipfs.UnixFsApi.GetAsync(dir.Id);
                Assert.Equal(3 * 512, tar.Length);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public async Task Isssue108()
        {
            var options = new AddFileOptions
            {
                Hash = "keccak-256",
                RawLeaves = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var other = await ipfs.UnixFsApi.ListFileAsync(node.Id);
            Assert.Equal(node.Id, other.Id);
            Assert.Equal(node.IsDirectory, other.IsDirectory);
            Assert.Equal(node.Size, other.Size);
        }

        [Fact]
        public async Task Read_SameFile_DifferentCids()
        {
            const string text = "\"hello world\" \r\n";
            var node = await ipfs.UnixFsApi.AddTextAsync(text);
            var cids = new[]
            {
                node.Id,
                new Cid
                {
                    ContentType = node.Id.ContentType,
                    Version = 1,
                    Encoding = node.Id.Encoding,
                    Hash = node.Id.Hash,
                },
                new Cid
                {
                    ContentType = node.Id.ContentType,
                    Version = 1,
                    Encoding = "base32",
                    Hash = node.Id.Hash,
                },
            };
            foreach (var cid in cids)
            {
                using (var cts = new CancellationTokenSource(3000))
                {
                    var got = await ipfs.UnixFsApi.ReadAllTextAsync(cid, cts.Token);
                    Assert.Equal(text, got);
                }
            }
        }

        internal static string MakeTemp()
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var x = Path.Combine(temp, "x");
            var xy = Path.Combine(x, "y");
            Directory.CreateDirectory(temp);
            Directory.CreateDirectory(x);
            Directory.CreateDirectory(xy);

            File.WriteAllText(Path.Combine(temp, "alpha.txt"), "alpha");
            File.WriteAllText(Path.Combine(temp, "beta.txt"), "beta");
            File.WriteAllText(Path.Combine(x, "x.txt"), "x");
            File.WriteAllText(Path.Combine(xy, "y.txt"), "y");
            return temp;
        }
    }
}
