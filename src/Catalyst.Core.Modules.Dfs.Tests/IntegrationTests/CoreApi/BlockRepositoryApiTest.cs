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

using System.IO;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public sealed class BlockRepositoryApiTest
    {
        private IDfsService ipfs;

        public BlockRepositoryApiTest(TestContext output)
        {
            ipfs = TestDfs.GetTestDfs(output);    
        }
        
        [Test]
        public void Exists() { Assert.NotNull(ipfs.BlockRepositoryApi); }

        [Test]
        public async Task Stats()
        {
            var stats = await ipfs.BlockRepositoryApi.StatisticsAsync();
            var version = await ipfs.BlockRepositoryApi.VersionAsync();
            Assert.AreEqual(stats.Version, version);
        }

        [Test]
        public async Task GarbageCollection()
        {
            var pinned = await ipfs.BlockApi.PutAsync(new byte[256], pin: true);
            var unpinned = await ipfs.BlockApi.PutAsync(new byte[512]);
            Assert.AreNotEqual(pinned, unpinned);
            Assert.NotNull(await ipfs.BlockApi.StatAsync(pinned));
            Assert.NotNull(await ipfs.BlockApi.StatAsync(unpinned));

            await ipfs.BlockRepositoryApi.RemoveGarbageAsync();
            Assert.NotNull(await ipfs.BlockApi.StatAsync(pinned));
            Assert.Null(await ipfs.BlockApi.StatAsync(unpinned));
        }

        [Test]
        public async Task Version_Info()
        {
            var versions = await ipfs.BlockRepositoryApi.VersionAsync();
            Assert.NotNull(versions);
         
            // Assert.True(versions.ContainsKey("Version"));
            // Assert.True(versions.ContainsKey("Repo"));
        }
        
        [Test]
        public async Task VersionFileMissing()
        {
            var versionPath = Path.Combine(ipfs.Options.Repository.ExistingFolder(), "version");
            var versionBackupPath = versionPath + ".bak";

            try
            {
                if (File.Exists(versionPath))
                {
                    File.Move(versionPath, versionBackupPath);
                }

                Assert.AreEqual("0", await ipfs.BlockRepositoryApi.VersionAsync());
            }
            finally
            {
                if (File.Exists(versionBackupPath))
                {
                    File.Move(versionBackupPath, versionPath);
                }
            }
        }
    }
}
