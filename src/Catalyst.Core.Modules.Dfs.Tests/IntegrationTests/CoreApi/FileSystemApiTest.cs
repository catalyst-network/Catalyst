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
            Assert.That(node.Id.ToString(), Is.EqualTo("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD"));
            Assert.That(node.Name, Is.EqualTo(""));
            Assert.That(node.Links.Count(), Is.EqualTo(0));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo("hello world"));

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
            Assert.That(node.Id.ToString(), Is.EqualTo("QmbFMke1KXqnYyBBWxB74N4c5SBnJMVAiMNRcGu6x1AwQH"));
            Assert.That(node.Name, Is.EqualTo(""));
            Assert.That(node.Links.Count(), Is.EqualTo(0));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo(""));

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
            Assert.That(node.Id.ToString(), Is.EqualTo("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD"));
            var pins = await ipfs.PinApi.ListAsync();
            pins.ToArray().Should().Contain(node.Id);

            options.Pin = false;
            node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That(node.Id.ToString(), Is.EqualTo("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD"));
            Assert.That(node.Links.Count(), Is.EqualTo(0));
            pins = await ipfs.PinApi.ListAsync();
            pins.ToArray().Should().NotContain(node.Id);
        }

        [Test]
        public async Task Add_SizeChunking()
        {
            var options = new AddFileOptions {ChunkSize = 3, Pin = true};
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            Assert.That(node.Id.ToString(), Is.EqualTo("QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn"));
            Assert.That(node.IsDirectory, Is.EqualTo(false));
            Assert.That(links.Length, Is.EqualTo(4));
            Assert.That(links[0].Id.ToString(), Is.EqualTo("QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6"));
            Assert.That(links[1].Id.ToString(), Is.EqualTo("QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN"));
            Assert.That(links[2].Id.ToString(), Is.EqualTo("QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6"));
            Assert.That(links[3].Id.ToString(), Is.EqualTo("QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE"));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo("hello world"));
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
            Assert.That(stream.Length, Is.EqualTo(11));
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
            Assert.That(node.Id.ToString(), Is.EqualTo("bafk2bzaceaswza5ss4iu2ia3galz6pyo6dfm5f4dmiw2lf2de22dmf4k533ba"));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo("hello world"));
        }

        [Test]
        public void AddFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var node = (UnixFsNode) ipfs.UnixFsApi.AddFileAsync(path).Result;
                Assert.That(node.Id.ToString(), Is.EqualTo("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD"));
                Assert.That(node.Links.Count(), Is.EqualTo(0));
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
                Assert.That(node.Id.Encoding, Is.EqualTo("base32"));
                Assert.That(node.Id.Version, Is.EqualTo(1));
                Assert.That(node.Links.Count(), Is.EqualTo(0));

                var text = ipfs.UnixFsApi.ReadAllTextAsync(node.Id).Result;
                Assert.That(text, Is.EqualTo("hello world"));
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

            Assert.That(node.Id.ToString(), Is.EqualTo("QmeZkAUfUFPq5YWGBan2ZYNd9k59DD1xW62pGJrU3C6JRo"));

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
                    Assert.That(n2, Is.EqualTo(n1));
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

            Assert.That(node.Id.ToString(), Is.EqualTo("QmeFhfB4g2GFbxYb7usApWzq8uC1vmuxJajFpiJiT5zLoy"));

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
                    Assert.That(n2, Is.EqualTo(n1));
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
                Assert.That(node.Id.ToString(), Is.EqualTo("QmNxvA5bwvPGgMXbmtyhxA1cKFdvQXnsGnZLCGor3AzYxJ"));
                Assert.That(node.IsDirectory, Is.EqualTo(true));
                Assert.That(node.Links.Count(), Is.EqualTo(1));
                Assert.That(node.Links.First().Name, Is.EqualTo("hello.txt"));
                Assert.That(node.Links.First().Id.ToString(), Is.EqualTo("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD"));
                Assert.That(node.Links.First().Size, Is.EqualTo(19));
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
            Assert.That(node.Id.ToString(), Is.EqualTo("bafkreifzjut3te2nhyekklss27nh3k72ysco7y32koao5eei66wof36n5e"));
            Assert.That(node.Size, Is.EqualTo(11));
            Assert.That(node.Links.Count(), Is.EqualTo(0));
            Assert.That(node.IsDirectory, Is.EqualTo(false));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo("hello world"));
        }

        [Test]
        public async Task Add_Inline()
        {
            var original = ipfs.Options.Block.AllowInlineCid;
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;

                var node = await ipfs.UnixFsApi.AddTextAsync("hiya");
                Assert.That(node.Id.Version, Is.EqualTo(1));
                Assert.That(node.Id.Hash.IsIdentityHash, Is.True);
                Assert.That(node.Size, Is.EqualTo(4));
                Assert.That(node.Links.Count(), Is.EqualTo(0));
                Assert.That(node.IsDirectory, Is.EqualTo(false));
                Assert.That(node.Id.Encode(), Is.EqualTo("bafyaadakbieaeeqenbuxsyiyaq"));
                var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
                Assert.That(text, Is.EqualTo("hiya"));
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
            Assert.That(node.Id.ToString(), Is.EqualTo("QmUuooB6zEhMmMaBvMhsMaUzar5gs5KwtVSFqG4C1Qhyhs"));
            Assert.That(node.IsDirectory, Is.EqualTo(false));
            Assert.That(links.Length, Is.EqualTo(4));
            Assert.That(links[0].Id.ToString(), Is.EqualTo("bafkreigwvapses57f56cfow5xvoua4yowigpwcz5otqqzk3bpcbbjswowe"));
            Assert.That(links[1].Id.ToString(), Is.EqualTo("bafkreiew3cvfrp2ijn4qokcp5fqtoknnmr6azhzxovn6b3ruguhoubkm54"));
            Assert.That(links[2].Id.ToString(), Is.EqualTo("bafkreibsybcn72tquh2l5zpim2bba4d2kfwcbpzuspdyv2breaq5efo7tq"));
            Assert.That(links[3].Id.ToString(), Is.EqualTo("bafkreihfuch72plvbhdg46lef3n5zwhnrcjgtjywjryyv7ffieyedccchu"));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo("hello world"));
        }

        [Test]
        public async Task Add_Protected()
        {
            var options = new AddFileOptions
            {
                ProtectionKey = "self"
            };
            var node = await ipfs.UnixFsApi.AddTextAsync("hello world", options);
            Assert.That(node.Id.ContentType, Is.EqualTo("cms"));
            Assert.That(node.Links.Count(), Is.EqualTo(0));
            Assert.That(node.IsDirectory, Is.EqualTo(false));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo("hello world"));
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
            Assert.That(node.Links.Count(), Is.EqualTo(4));
            Assert.That(node.IsDirectory, Is.EqualTo(false));

            var text = await ipfs.UnixFsApi.ReadAllTextAsync(node.Id);
            Assert.That(text, Is.EqualTo("hello world"));
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
            Assert.That(links.Length, Is.EqualTo(nodes.Length - 1));
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
                Assert.That(await ipfs.UnixFsApi.ReadAllTextAsync(node.Id), Is.EqualTo(text));

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

                Assert.That(lastProgress.Bytes, Is.EqualTo(11UL));
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
                Assert.That(files.Length, Is.EqualTo(2));
                Assert.That(files[0].Name, Is.EqualTo("alpha.txt"));
                Assert.That(files[1].Name, Is.EqualTo("beta.txt"));

                Assert.That(ipfs.UnixFsApi.ReadAllTextAsync(files[0].Id).Result, Is.EqualTo("alpha"));
                Assert.That(ipfs.UnixFsApi.ReadAllTextAsync(files[1].Id).Result, Is.EqualTo("beta"));

                Assert.That(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/alpha.txt").Result, Is.EqualTo("alpha"));
                Assert.That(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/beta.txt").Result, Is.EqualTo("beta"));
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
                Assert.That(files.Length, Is.EqualTo(3));
                Assert.That(files[0].Name, Is.EqualTo("alpha.txt"));
                Assert.That(files[1].Name, Is.EqualTo("beta.txt"));
                Assert.That(files[2].Name, Is.EqualTo("x"));
                Assert.That(files[0].Size, Is.Not.EqualTo(0));
                Assert.That(files[1].Size, Is.Not.EqualTo(0));

                var rootFiles = ipfs.UnixFsApi.ListFileAsync(dir.Id).Result.Links.ToArray();
                Assert.That(rootFiles.Length, Is.EqualTo(3));
                Assert.That(rootFiles[0].Name, Is.EqualTo("alpha.txt"));
                Assert.That(rootFiles[1].Name, Is.EqualTo("beta.txt"));
                Assert.That(rootFiles[2].Name, Is.EqualTo("x"));

                var xfiles = ipfs.UnixFsApi.ListFileAsync(rootFiles[2].Id).Result.Links.ToArray();
                Assert.That(xfiles.Length, Is.EqualTo(2));
                Assert.That(xfiles[0].Name, Is.EqualTo("x.txt"));
                Assert.That(xfiles[1].Name, Is.EqualTo("y"));

                var yfiles = ipfs.UnixFsApi.ListFileAsync(xfiles[1].Id).Result.Links.ToArray();
                Assert.That(yfiles.Length, Is.EqualTo(1));
                Assert.That(yfiles[0].Name, Is.EqualTo("y.txt"));

                Assert.That(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/x/x.txt").Result, Is.EqualTo("x"));
                Assert.That(ipfs.UnixFsApi.ReadAllTextAsync(dir.Id + "/x/y/y.txt").Result, Is.EqualTo("y"));
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
                Assert.That(dir.Id.Hash.Algorithm.Name, Is.EqualTo(alg));

                foreach (var link in dir.Links)
                {
                    Assert.That(link.Id.Hash.Algorithm.Name, Is.EqualTo(alg));
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

                Assert.That(allLinks.Count, Is.EqualTo(6));
                Assert.That(allLinks[0].Name, Is.EqualTo("alpha.txt"));
                Assert.That(allLinks[1].Name, Is.EqualTo("beta.txt"));
                Assert.That(allLinks[2].Name, Is.EqualTo("x"));
                Assert.That(allLinks[3].Name, Is.EqualTo("x.txt"));
                Assert.That(allLinks[4].Name, Is.EqualTo("y"));
                Assert.That(allLinks[5].Name, Is.EqualTo("y.txt"));
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
                        Assert.That(content, Is.EqualTo("some content"));
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

                Assert.Multiple(() =>
                {
                    Assert.That(files[0], Is.EqualTo($"{dirid}"));
                    Assert.That(files[1], Is.EqualTo($"{dirid}/alpha.txt"));
                    Assert.That(files[2], Is.EqualTo($"{dirid}/beta.txt"));
                    Assert.That(files[3], Is.EqualTo($"{dirid}/x"));
                    Assert.That(files[4], Is.EqualTo($"{dirid}/x/x.txt"));
                    Assert.That(files[5], Is.EqualTo($"{dirid}/x/y"));
                    Assert.That(files[6], Is.EqualTo($"{dirid}/x/y/y.txt"));
                });
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

                Assert.Multiple(() =>
                {
                    Assert.That(files[0], Is.EqualTo($"{dirid}"));
                    Assert.That(files[1], Is.EqualTo($"{dirid}/alpha.txt"));
                    Assert.That(files[2], Is.EqualTo($"{dirid}/beta.txt"));
                    Assert.That(files[3], Is.EqualTo($"{dirid}/x"));
                    Assert.That(files[4], Is.EqualTo($"{dirid}/x/x.txt"));
                    Assert.That(files[5], Is.EqualTo($"{dirid}/x/y"));
                    Assert.That(files[6], Is.EqualTo($"{dirid}/x/y/y.txt"));
                });
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
                Assert.That(tar.Length, Is.EqualTo(3 * 512));
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
                using var cts = new CancellationTokenSource(3000);

                var got = await ipfs.UnixFsApi.ReadAllTextAsync(cid, cts.Token);
                Assert.That(got, Is.EqualTo(text));
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
