using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.KBucket
{
    [TestClass]
    public class DistanceTest
    {
        /// <summary>
        ///  From https://github.com/tristanls/k-bucket/blob/master/test/defaultDistance.js
        /// </summary>
        [TestMethod]
        public void Tristanls()
        {
            var bucket = new KBucket<Contact>();

            Assert.AreEqual((ulong) 0, bucket.Distance(new byte[] {0x00}, new byte[] {0x00}));
            Assert.AreEqual((ulong) 1, bucket.Distance(new byte[] {0x00}, new byte[] {0x01}));
            Assert.AreEqual((ulong) 3, bucket.Distance(new byte[] {0x02}, new byte[] {0x01}));
            Assert.AreEqual((ulong) 255, bucket.Distance(new byte[] {0x00}, new byte[] {0x00, 0x00}));
            Assert.AreEqual((ulong) 16640, bucket.Distance(new byte[] {0x01, 0x24}, new byte[] {0x40, 0x24}));
        }

        [TestMethod]
        public void ByContact()
        {
            var bucket = new KBucket<Contact>();
            var c0 = new Contact((byte) 0);
            var c1 = new Contact((byte) 1);
            var c2 = new Contact((byte) 2);
            var c00 = new Contact((byte) 0, (byte) 0);
            var c0124 = new Contact((byte) 0x01, (byte) 0x24);
            var c4024 = new Contact((byte) 0x40, (byte) 0x24);
            Assert.AreEqual((ulong) 0, bucket.Distance(c0, c0));
            Assert.AreEqual((ulong) 1, bucket.Distance(c0, c1));
            Assert.AreEqual((ulong) 3, bucket.Distance(c2, c1));
            Assert.AreEqual((ulong) 255, bucket.Distance(c0, c00));
            Assert.AreEqual((ulong) 16640, bucket.Distance(c0124, c4024));
        }
    }
}
