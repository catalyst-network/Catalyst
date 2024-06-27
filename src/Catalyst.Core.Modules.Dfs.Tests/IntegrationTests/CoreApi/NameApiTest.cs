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
using System.IO;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.TestUtils;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class NameApiTest
    {
        private IDfsService ipfs;

        public NameApiTest()
        {
            ipfs = TestDfs.GetTestDfs(null, "sha2-256");
        }

        [Test]
        public async Task Resolve_Cid()
        {
            var actual = await ipfs.NameApi.ResolveAsync("QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.AreEqual("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);

            actual = await ipfs.NameApi.ResolveAsync("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.AreEqual("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);
        }

        [Test]
        public async Task Resolve_Cid_Path()
        {
            var temp = FileSystemApiTest.MakeTemp();
            try
            {
                var dir = await ipfs.UnixFsApi.AddDirectoryAsync(temp);
                var name = "/ipfs/" + dir.Id.Encode() + "/x/y/y.txt";
                Assert.AreEqual("/ipfs/QmTwEE2eSyzcvUctxP2negypGDtj7DQDKVy8s3Rvp6y6Pc",
                    await ipfs.NameApi.ResolveAsync(name));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Test]
        public void Resolve_Cid_Invalid()
        {
            ExceptionAssert.Throws<FormatException>(() =>
            {
                var _ = ipfs.NameApi.ResolveAsync("QmHash").Result;
            });
        }

        [Test]
        public async Task Resolve_DnsLink()
        {
            var iopath = await ipfs.NameApi.ResolveAsync("ipfs.io");
            Assert.NotNull(iopath);

            var path = await ipfs.NameApi.ResolveAsync("/ipns/ipfs.io");
            Assert.AreEqual(iopath, path);
        }

        [Test]
        public async Task Resolve_DnsLink_Recursive()
        {
            var path = await ipfs.NameApi.ResolveAsync("/ipns/ipfs.io/media", true);
            Assert.That(path, Does.StartWith("/ipfs/"));
            Assert.That(path, Does.EndWith("/media"));

            path = await ipfs.NameApi.ResolveAsync("ipfs.io/media", true);
            Assert.That(path, Does.StartWith("/ipfs/"));
            Assert.That(path, Does.EndWith("/media"));

            path = await ipfs.NameApi.ResolveAsync("/ipfs.io/media", true);
            Assert.That(path, Does.StartWith("/ipfs/"));
            Assert.That(path, Does.EndWith("/media"));
        }

        [Test]
        public void Resolve_NoDnsLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.DnsApi.ResolveAsync("google.com").Result;
            });
        }

        //[Test]
        //[Ignore("Need a working IPNS")]
        //public async Task Resolve_DnsLink_Recursive()
        //{
        //    var ipfs = TestFixture.Ipfs;

        //    var media = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media");
        //    var actual = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media", recursive: true);
        //    Assert.AreNotEqual(media, actual);
        //}
    }
}
