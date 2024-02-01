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
using Catalyst.Core.Modules.Dfs.UnixFs;
using Catalyst.TestUtils;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Tar;
using Lib.P2P;
using Lib.P2P.Cryptography;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class FileSystemApiTest
    {
        private IDfsService ipfs;

        [SetUp]
        public void Init()
        {
            ipfs = TestDfs.GetTestDfs(null, "sha2-256");
        }

        [TearDown]
        public void TearDown()
        {
            ipfs.Dispose();
        }

        [Test]
        public async Task AddText()
        {
            var node = (UnixFsNode) await ipfs.UnixFsApi.AddTextAsync("hello world");
            Assert.That("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", Is.EqualTo(node.Id.ToString()));
            Assert.That("", Is.EqualTo(node.Name));
            Assert.That(0, Is.EqualTo(node.Links.Count()));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("hello world", Is.EqualTo(text));

            var actual = await ipfs.UnixFsApi.ListFileAsync(node.Id);
            Assert.That(node.Id, Is.EqualTo(actual.Id));
            Assert.That(node.IsDirectory, Is.EqualTo(actual.IsDirectory));
            Assert.That(node.Links.Count(), Is.EqualTo(actual.Links.Count()));
            Assert.That(node.Size, Is.EqualTo(actual.Size));
        }

        [Test]
        public async Task AddEmptyText()
        {
            var node = (UnixFsNode) await ipfs.UnixFsApi.AddTextAsync("");
            Assert.That("QmbFMke1KXqnYyBBWxB74N4c5SBnJMVAiMNRcGu6x1AwQH", Is.EqualTo(node.Id.ToString()));
            Assert.That("", Is.EqualTo(node.Name));
            Assert.That(0, Is.EqualTo(node.Links.Count()));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("", Is.EqualTo(text));

            var actual = await ipfs.UnixFsApi.ListFileAsync(node.Id);
            Assert.That(node.Id, Is.EqualTo(actual.Id));
            Assert.That(node.IsDirectory, Is.EqualTo(actual.IsDirectory));
            Assert.That(node.Links.Count(), Is.EqualTo(actual.Links.Count()));
            Assert.That(node.Size, Is.EqualTo(actual.Size));
        }

        [Test]
        public async Task AddEmpty_Check_Object()
        {
            // see https://github.com/ipfs/js-ipfs-unixfs/pull/25
            var node = await ipfs.UnixFsApi.AddTextAsync("");
            var block = await ipfs.ObjectApi.GetAsync(node.Id);
            var expected = new byte[] {0x08, 0x02, 0x18, 0x00};
            Assert.That(node.Id, Is.EqualTo(block.Id));
            Assert.That(expected, Is.EqualTo(block.DataBytes));
        }

        [Test]
        public async Task AddDuplicateWithPin()
        {
            var options = new AddFileOptions
            {
                Pin = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", Is.EqualTo(node.Id.ToString()));
            var pins = await ipfs.PinApi.ListAsync();
            pins.ToArray().Should().Contain(node.Id);

            options.Pin = false;
            node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", Is.EqualTo(node.Id.ToString()));
            Assert.That(0, Is.EqualTo(node.Links.Count()));
            pins = await ipfs.PinApi.ListAsync();
            pins.ToArray().Should().NotContain(node.Id);
        }

        [Test]
        public async Task Add_SizeChunking()
        {
            var options = new AddFileOptions {ChunkSize = 3, Pin = true};
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.That("QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn", Is.EqualTo(node.Id.ToString()));
            Assert.That(false, Is.EqualTo(node.IsDirectory));
            Assert.That(4, Is.EqualTo(links.Length));
            Assert.That("QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6", Is.EqualTo(links[0].Id.ToString()));
            Assert.That("QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN", Is.EqualTo(links[1].Id.ToString()));
            Assert.That("QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6", Is.EqualTo(links[2].Id.ToString()));
            Assert.That("QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE", Is.EqualTo(links[3].Id.ToString()));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("hello world", Is.EqualTo(text));
        }

        [Test]
        public async Task StreamBehaviour()
        {
            var options = new AddFileOptions
            {
                ChunkSize = 3,
                Pin = true,
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var stream = await ipfs.UnixFsApi.ReadFileAsync(node.Id);
            Assert.That(11, Is.EqualTo(stream.Length));
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.CanWrite, Is.False);
            Assert.That(stream.CanSeek, Is.True);
        }

        [Test]
        public async Task Add_HashAlgorithm()
        {
            var options = new AddFileOptions
            {
                Hash = "blake2b-256",
                RawLeaves = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That("bafk2bzaceaswza5ss4iu2ia3galz6pyo6dfm5f4dmiw2lf2de22dmf4k533ba", Is.EqualTo(node.Id.ToString()));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("hello world", Is.EqualTo(text));
        }

        [Test]
        public void AddFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var node = (UnixFsNode) ipfs.UnixFsApi.AddFileAsync(path).Result;
                Assert.That("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", Is.EqualTo(node.Id.ToString()));
                Assert.That(0, Is.EqualTo(node.Links.Count()));
                Assert.That(Path.GetFileName(path), Is.EqualTo(node.Name));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
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
                Assert.That("base32", Is.EqualTo(node.Id.Encoding));
                Assert.That(1, Is.EqualTo(node.Id.Version));
                Assert.That(0, Is.EqualTo(node.Links.Count()));

                var text = ipfs.UnixFsApi.ReadAllTextAsync(node.Id).Result;
                Assert.That("hello world", Is.EqualTo(text));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void AddFile_Large()
        {
            AddFile(); // warm up

            var path = "star_trails.mp4";
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var node = ipfs.UnixFsApi.AddFileAsync(path).Result;
            stopWatch.Stop();
            
            // _testOutputHelper.WriteLine("Add file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);

            Assert.That("QmeZkAUfUFPq5YWGBan2ZYNd9k59DD1xW62pGJrU3C6JRo", Is.EqualTo(node.Id.ToString()));

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
                    Assert.That(n1, Is.EqualTo(n2));
                    if (n1 == 0)
                    {
                        break;
                    }
                    
                    for (var i = 0; i < n1; ++i)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            throw new Exception("data not the same");
                        }
                    }
                }
            }

            stopWatch.Stop();
            
            // _testOutputHelper.WriteLine("Readfile file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);
        }

        /// <seealso href="https://github.com/richardschneider/net-ipfs-engine/issues/125"/>
        [Test]
        public void AddFile_Larger()
        {
            AddFile(); // warm up

            var path = "starx2.mp4";
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var node = ipfs.UnixFsApi.AddFileAsync(path).Result;
            stopWatch.Stop();
            TestContext.WriteLine("Add file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);

            Assert.That("QmeFhfB4g2GFbxYb7usApWzq8uC1vmuxJajFpiJiT5zLoy", Is.EqualTo(node.Id.ToString()));

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
                    Assert.That(n1, Is.EqualTo(n2));
                    if (n1 == 0)
                    {
                        break;
                    }
                    
                    for (var i = 0; i < n1; ++i)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            throw new Exception("data not the same");
                        }
                    }
                }
            }

            stopWatch.Stop();
            TestContext.WriteLine("Readfile file took {0} seconds.", stopWatch.Elapsed.TotalSeconds);
        }

        [Test]
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
                Assert.That("QmNxvA5bwvPGgMXbmtyhxA1cKFdvQXnsGnZLCGor3AzYxJ", Is.EqualTo(node.Id.ToString()));
                Assert.That(true, Is.EqualTo(node.IsDirectory));
                Assert.That(1, Is.EqualTo(node.Links.Count()));
                Assert.That("hello.txt", Is.EqualTo(node.Links.First().Name));
                Assert.That("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", Is.EqualTo(node.Links.First().Id.ToString()));
                Assert.That(19, Is.EqualTo(node.Links.First().Size));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public async Task Add_Raw()
        {
            var options = new AddFileOptions
            {
                RawLeaves = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That("bafkreifzjut3te2nhyekklss27nh3k72ysco7y32koao5eei66wof36n5e", Is.EqualTo(node.Id.ToString()));
            Assert.That(11, Is.EqualTo(node.Size));
            Assert.That(0, Is.EqualTo(node.Links.Count()));
            Assert.That(false, Is.EqualTo(node.IsDirectory));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("hello world", Is.EqualTo(text));
        }

        [Test]
        public async Task Add_Inline()
        {
            var original = ipfs.Options.Block.AllowInlineCid;
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;

                var node = await ipfs.UnixFsApi.AddTextAsync("hiya");
                Assert.That(1, Is.EqualTo(node.Id.Version));
                Assert.That(node.Id.Hash.IsIdentityHash, Is.True);
                Assert.That(4, Is.EqualTo(node.Size));
                Assert.That(0, Is.EqualTo(node.Links.Count()));
                Assert.That(false, Is.EqualTo(node.IsDirectory));
                Assert.That("bafyaadakbieaeeqenbuxsyiyaq", Is.EqualTo(node.Id.Encode()));
                var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
                Assert.That("hiya", Is.EqualTo(text));
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = original;
            }
        }

        [Test]
        public async Task Add_RawAndChunked()
        {
            var options = new AddFileOptions
            {
                RawLeaves = true,
                ChunkSize = 3
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.That("QmUuooB6zEhMmMaBvMhsMaUzar5gs5KwtVSFqG4C1Qhyhs", Is.EqualTo(node.Id.ToString()));
            Assert.That(false, Is.EqualTo(node.IsDirectory));
            Assert.That(4, Is.EqualTo(links.Length));
            Assert.That("bafkreigwvapses57f56cfow5xvoua4yowigpwcz5otqqzk3bpcbbjswowe", Is.EqualTo(links[0].Id.ToString()));
            Assert.That("bafkreiew3cvfrp2ijn4qokcp5fqtoknnmr6azhzxovn6b3ruguhoubkm54", Is.EqualTo(links[1].Id.ToString()));
            Assert.That("bafkreibsybcn72tquh2l5zpim2bba4d2kfwcbpzuspdyv2breaq5efo7tq", Is.EqualTo(links[2].Id.ToString()));
            Assert.That("bafkreihfuch72plvbhdg46lef3n5zwhnrcjgtjywjryyv7ffieyedccchu", Is.EqualTo(links[3].Id.ToString()));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("hello world", Is.EqualTo(text));
        }

        [Test]
        public async Task Add_Protected()
        {
            var options = new AddFileOptions
            {
                ProtectionKey = "self"
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That("cms", Is.EqualTo(node.Id.ContentType));
            Assert.That(0, Is.EqualTo(node.Links.Count()));
            Assert.That(false, Is.EqualTo(node.IsDirectory));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("hello world", Is.EqualTo(text));
        }

        [Test]
        public async Task Add_Protected_Chunked()
        {
            var options = new AddFileOptions
            {
                ProtectionKey = "self",
                ChunkSize = 3
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That(4, Is.EqualTo(node.Links.Count()));
            Assert.That(false, Is.EqualTo(node.IsDirectory));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That("hello world", Is.EqualTo(text));
        }

        [Test]
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
            Assert.That(nodes[0], Is.EqualTo(node.Id.ToString()));
            Assert.That(nodes.Length - 1, Is.EqualTo(links.Length));
            for (var i = 0; i < links.Length; ++i)
            {
                Assert.That(nodes[i + 1], Is.EqualTo(links[i].Id.ToString()));
            }

            // TODO: Need a method to test that the CId is not held locally.
            //foreach (var n in nodes)
            //{
            //    Assert.Null(await ipfs.Block.StatAsync(n));
            //}
        }

        [Test]
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
                        Assert.That(text.Substring(offset), Is.EqualTo(readData));
                    }
                }
            }
        }

        [Test]
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
                            Assert.That(text.Substring(offset, Math.Min(11 - offset, length)), Is.EqualTo(readData));
                        }
                    }
                }
            }
        }

        [Test]
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
                        Assert.That(text.Substring(0, Math.Min(11, length)), Is.EqualTo(readData));
                    }
                }
            }
        }

        [Test]
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
                            Assert.That(text.Substring(offset, Math.Min(11 - offset, length)), Is.EqualTo(readData));
                        }
                    }
                }
            }
        }

        [Test]
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
                            Assert.That(text.Substring(offset, Math.Min(11 - offset, length)), Is.EqualTo(readData));
                        }
                    }
                }
            }
        }

        [Test]
        public async Task Read_ProtectedMissingKey()
        {
            const string text = "hello world";
            var key = await ipfs.KeyApi.CreateAsync("alice", "rsa", 512);
            try
            {
                var options = new AddFileOptions {ProtectionKey = key.Name};
                var node = await ipfs.UnixFsApi.AddTextAsync(text, options);
                Assert.That(text, Is.EqualTo(await ipfs.UnixFsApi.ReadAllTextAsync(node.Id)));

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

        [Test]
        [Ignore("To be fixed in issue #1241")]
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

                Assert.That(11UL, Is.EqualTo(lastProgress.Bytes));
                Assert.That(Path.GetFileName(path), Is.EqualTo(lastProgress.Name));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void AddDirectory()
        {
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp, false).Result;
                Assert.That(dir.IsDirectory, Is.True);

                var files = dir.Links.ToArray();
                Assert.That(2, Is.EqualTo(files.Length));
                Assert.That("alpha.txt", Is.EqualTo(files[0].Name));
                Assert.That("beta.txt", Is.EqualTo(files[1].Name));

                Assert.That("alpha", Is.EqualTo(ipfs.UnixFsApi.ReadAllTextAsync(files[0].Id).Result));
                Assert.That("beta", Is.EqualTo(ipfs.UnixFsApi.ReadAllTextAsync(files[1].Id).Result));

                Assert.That("alpha", Is.EqualTo(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/alpha.txt").Result));
                Assert.That("beta", Is.EqualTo(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/beta.txt").Result));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public void AddDirectoryRecursive()
        {
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp).Result;
                Assert.That(dir.IsDirectory, Is.True);

                var files = dir.Links.ToArray();
                Assert.That(3, Is.EqualTo(files.Length));
                Assert.That("alpha.txt", Is.EqualTo(files[0].Name));
                Assert.That("beta.txt", Is.EqualTo(files[1].Name));
                Assert.That("x", Is.EqualTo(files[2].Name));
                Assert.That(0, Is.Not.EqualTo(files[0].Size));
                Assert.That(0, Is.Not.EqualTo(files[1].Size));

                var rootFiles = ipfs.UnixFsApi.ListFileAsync(dir.Id).Result.Links.ToArray();
                Assert.That(3, Is.EqualTo(rootFiles.Length));
                Assert.That("alpha.txt", Is.EqualTo(rootFiles[0].Name));
                Assert.That("beta.txt", Is.EqualTo(rootFiles[1].Name));
                Assert.That("x", Is.EqualTo(rootFiles[2].Name));

                var xfiles = ipfs.UnixFsApi.ListFileAsync(rootFiles[2].Id).Result.Links.ToArray();
                Assert.That(2, Is.EqualTo(xfiles.Length));
                Assert.That("x.txt", Is.EqualTo(xfiles[0].Name));
                Assert.That("y", Is.EqualTo(xfiles[1].Name));

                var yfiles = ipfs.UnixFsApi.ListFileAsync(xfiles[1].Id).Result.Links.ToArray();
                Assert.That(1, Is.EqualTo(yfiles.Length));
                Assert.That("y.txt", Is.EqualTo(yfiles[0].Name));

                Assert.That("x", Is.EqualTo(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/x/x.txt").Result));
                Assert.That("y", Is.EqualTo(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/x/y/y.txt").Result));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public void AddDirectory_WithHashAlgorithm()
        {
            const string alg = "keccak-512";
            var options = new AddFileOptions {Hash = alg};
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp, false, options).Result;
                Assert.That(dir.IsDirectory, Is.True);
                Assert.That(alg, Is.EqualTo(dir.Id.Hash.Algorithm.Name));

                foreach (var link in dir.Links)
                {
                    Assert.That(alg, Is.EqualTo(link.Id.Hash.Algorithm.Name));
                }
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public void AddDirectory_WithCidEncoding()
        {
            var encoding = "base32z";
            var options = new AddFileOptions {Encoding = encoding};
            var temp = MakeTemp();
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp, false, options).Result;
                Assert.That(dir.IsDirectory, Is.True);
                Assert.That(encoding, Is.EqualTo(dir.Id.Encoding));

                foreach (var link in dir.Links)
                {
                    Assert.That(encoding, Is.EqualTo(link.Id.Encoding));
                }
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public async Task AddDirectoryRecursive_ObjectLinks()
        {
            var temp = MakeTemp();
            try
            {
                var dir = await ipfs.UnixFsApi.AddDirectoryAsync(temp);
                Assert.That(dir.IsDirectory, Is.True);

                var cid = dir.Id;
                var i = 0;
                var allLinks = new List<IMerkleLink>();
                while (cid != null)
                {
                    var links = await ipfs.ObjectApi.LinksAsync(cid);
                    allLinks.AddRange(links);
                    cid = i < allLinks.Count ? allLinks[i++].Id : null;
                }

                Assert.That(6, Is.EqualTo(allLinks.Count));
                Assert.That("alpha.txt", Is.EqualTo(allLinks[0].Name));
                Assert.That("beta.txt", Is.EqualTo(allLinks[1].Name));
                Assert.That("x", Is.EqualTo(allLinks[2].Name));
                Assert.That("x.txt", Is.EqualTo(allLinks[3].Name));
                Assert.That("y", Is.EqualTo(allLinks[4].Name));
                Assert.That("y.txt", Is.EqualTo(allLinks[5].Name));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        // [Test]
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

        [Test]
        public async Task Read_From_OtherNode()
        {
            using (var a = TestDfs.GetTestDfs())
            {
                using (var b = TestDfs.GetTestDfs())
                {
                    using (var c = TestDfs.GetTestDfs())
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
                        TestContext.WriteLine($"B is {b.LocalPeer}");

                        // Node that has the content.
                        c.Options.Discovery.DisableMdns = true;
                        c.Options.Swarm.MinConnections = 0;
                        c.Options.Swarm.PrivateNetworkKey = psk;
                        c.Options.Discovery.BootstrapPeers = bootstrapPeers;
                        await c.StartAsync();
                        await c.SwarmApi.ConnectAsync(bootstrapPeers[0]);
                        TestContext.WriteLine($"C is {c.LocalPeer}");

                        var fsn = await c.UnixFsApi.AddTextAsync("some content");
                        var cid = fsn.Id;

                        // Node that reads the content.
                        a.Options.Discovery.DisableMdns = true;
                        a.Options.Swarm.MinConnections = 0;
                        a.Options.Swarm.PrivateNetworkKey = psk;
                        a.Options.Discovery.BootstrapPeers = bootstrapPeers;
                        await a.StartAsync();
                        TestContext.WriteLine($"A is {a.LocalPeer}");
                        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var content = await a.UnixFsApi.ReadAllTextAsync(cid, cts.Token);
                        Assert.That("some content", Is.EqualTo(content));
                    }
                }
            }
        }

        [Test]
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

                Assert.That($"{dirid}", Is.EqualTo(files[0]));
                Assert.That($"{dirid}/alpha.txt", Is.EqualTo(files[1]));
                Assert.That($"{dirid}/beta.txt", Is.EqualTo(files[2]));
                Assert.That($"{dirid}/x", Is.EqualTo(files[3]));
                Assert.That($"{dirid}/x/x.txt", Is.EqualTo(files[4]));
                Assert.That($"{dirid}/x/y", Is.EqualTo(files[5]));
                Assert.That($"{dirid}/x/y/y.txt", Is.EqualTo(files[6]));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
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

                Assert.That($"{dirid}", Is.EqualTo(files[0]));
                Assert.That($"{dirid}/alpha.txt", Is.EqualTo(files[1]));
                Assert.That($"{dirid}/beta.txt", Is.EqualTo(files[2]));
                Assert.That($"{dirid}/x", Is.EqualTo(files[3]));
                Assert.That($"{dirid}/x/x.txt", Is.EqualTo(files[4]));
                Assert.That($"{dirid}/x/y", Is.EqualTo(files[5]));
                Assert.That($"{dirid}/x/y/y.txt", Is.EqualTo(files[6]));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public async Task GetTar_EmptyDirectory()
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(temp);
            try
            {
                var dir = ipfs.UnixFsApi.AddDirectoryAsync(temp).Result;
                var _ = dir.Id.Encode();

                var tar = await ipfs.UnixFsApi.GetAsync(dir.Id);
                Assert.That(3 * 512, Is.EqualTo(tar.Length));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public async Task Isssue108()
        {
            var options = new AddFileOptions
            {
                Hash = "keccak-256",
                RawLeaves = true
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var other = await ipfs.UnixFsApi.ListFileAsync(node.Id);
            Assert.That(node.Id, Is.EqualTo(other.Id));
            Assert.That(node.IsDirectory, Is.EqualTo(other.IsDirectory));
            Assert.That(node.Size, Is.EqualTo(other.Size));
        }

        [Test]
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
                    Assert.That(text, Is.EqualTo(got));
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
