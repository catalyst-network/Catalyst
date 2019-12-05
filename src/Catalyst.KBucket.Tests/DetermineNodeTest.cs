using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.KBucket
{
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/determineNode.js
    /// </summary>
    [TestClass]
    public class DetermineNodeTest
    {
        private static readonly Bucket<Contact> Left = new Bucket<Contact>();
        private static readonly Bucket<Contact> Right = new Bucket<Contact>();
        private static readonly Bucket<Contact> Root = new Bucket<Contact> {Left = Left, Right = Right};

        [TestMethod]
        public void Tests()
        {
            var kBucket = new KBucket<Contact>();
            Bucket<Contact> actual;

            actual = kBucket._DetermineNode(Root, new byte[] {0x00}, 0);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 0);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 1);
            Assert.AreSame(Right, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 2);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x40}, 9);
            Assert.AreSame(Left, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x41}, 7);
            Assert.AreSame(Right, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x41, 0x00}, 7);
            Assert.AreSame(Right, actual);

            actual = kBucket._DetermineNode(Root, new byte[] {0x00, 0x41, 0x00}, 15);
            Assert.AreSame(Right, actual);
        }
    }
}
