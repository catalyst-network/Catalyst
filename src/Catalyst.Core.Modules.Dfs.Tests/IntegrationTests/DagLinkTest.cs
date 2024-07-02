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
using Catalyst.Core.Lib.Dag;
using Catalyst.TestUtils;
using Google.Protobuf;
using MultiFormats;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class DagLinkTest
    {
        [Test]
        public void Creating()
        {
            var link = new DagLink("abc", "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", 5);
            Assert.That(link.Name, Is.EqualTo("abc"));
            Assert.That(link.Id.ToString(), Is.EqualTo("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V"));
            Assert.That(link.Size, Is.EqualTo(5));
        }

        [Test]
        public void Cloning()
        {
            var link = new DagLink("abc", "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", 5);
            var clone = new DagLink(link);

            Assert.That(clone.Name, Is.EqualTo("abc"));
            Assert.That(clone.Id.ToString(), Is.EqualTo("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V"));
            Assert.That(clone.Size, Is.EqualTo(5));
        }

        [Test]
        public void Encoding()
        {
            const string encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd51201611803";
            var link = new DagLink("a", "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            link.ToArray();
            Assert.That(link.ToArray().ToHexString(), Is.EqualTo(encoded));
        }

        [Test]
        public void Encoding_EmptyName()
        {
            var encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd512001803";
            var link = new DagLink("", "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            link.ToArray();
            Assert.That(encoded, Is.EqualTo(link.ToArray().ToHexString()));
        }

        [Test]
        public void Encoding_NullName()
        {
            const string encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd51803";
            var link = new DagLink(null, "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            link.ToArray();
            Assert.That(link.ToArray().ToHexString(), Is.EqualTo(encoded));
        }

        [Test]
        public void Null_Stream()
        {
            TestUtils.ExceptionAssert.Throws(() => new DagLink((CodedInputStream) null));
            TestUtils.ExceptionAssert.Throws(() => new DagLink((Stream) null));
        }

        [Test]
        public void Cid_V1()
        {
            var link = new DagLink("hello",
                "zB7NCdng5WffuNCgHu4PhDj7nbtuVrhPc2pMhanNxYKRsECdjX9nd44g6CRu2xNrj2bG2NNaTsveL5zDGWhbfiug3VekW", 11);
            Assert.That(link.Name, Is.EqualTo("hello"));
            Assert.That(link.Id.Version, Is.EqualTo(1));
            Assert.That(link.Id.ContentType, Is.EqualTo("raw"));
            Assert.That(link.Id.Hash.Algorithm.Name, Is.EqualTo("sha2-512"));
            Assert.That(link.Size, Is.EqualTo(11));
        }
    }
}
