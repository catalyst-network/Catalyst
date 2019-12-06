using System.IO;
using Google.Protobuf;
using MultiFormats;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    public class DagLinkTest
    {
        [Fact]
        public void Creating()
        {
            var link = new DagLink("abc", "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", 5);
            Assert.Equal("abc", link.Name);
            Assert.Equal("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", (string) link.Id);
            Assert.Equal(5, link.Size);
        }

        [Fact]
        public void Cloning()
        {
            var link = new DagLink("abc", "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", 5);
            var clone = new DagLink(link);

            Assert.Equal("abc", clone.Name);
            Assert.Equal("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", (string) clone.Id);
            Assert.Equal(5, clone.Size);
        }

        [Fact]
        public void Encoding()
        {
            var encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd51201611803";
            var link = new DagLink("a", "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            var x = link.ToArray();
            Assert.Equal(encoded, link.ToArray().ToHexString());
        }

        [Fact]
        public void Encoding_EmptyName()
        {
            var encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd512001803";
            var link = new DagLink("", "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            var x = link.ToArray();
            Assert.Equal(encoded, link.ToArray().ToHexString());
        }

        [Fact]
        public void Encoding_NullName()
        {
            var encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd51803";
            var link = new DagLink(null, "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            var x = link.ToArray();
            Assert.Equal(encoded, link.ToArray().ToHexString());
        }

        [Fact]
        public void Null_Stream()
        {
            TestUtils.ExceptionAssert.Throws(() => new DagLink((CodedInputStream) null));
            TestUtils.ExceptionAssert.Throws(() => new DagLink((Stream) null));
        }

        [Fact]
        public void Cid_V1()
        {
            var link = new DagLink("hello",
                "zB7NCdng5WffuNCgHu4PhDj7nbtuVrhPc2pMhanNxYKRsECdjX9nd44g6CRu2xNrj2bG2NNaTsveL5zDGWhbfiug3VekW", 11);
            Assert.Equal("hello", link.Name);
            Assert.Equal(1, link.Id.Version);
            Assert.Equal("raw", link.Id.ContentType);
            Assert.Equal("sha2-512", link.Id.Hash.Algorithm.Name);
            Assert.Equal(11, link.Size);
        }
    }
}
