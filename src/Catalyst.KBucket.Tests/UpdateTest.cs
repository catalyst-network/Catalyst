using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.KBucket
{
    /// <summary>
    ///   From https://github.com/tristanls/k-bucket/blob/master/test/update.js
    /// </summary>
    [TestClass]
    public class UpdateTest
    {
        [TestMethod]
        public void ContactDrop()
        {
            var kBucket = new KBucket<Contact>
            {
                Arbiter = (a, b) => a.Clock > b.Clock ? a : b
            };
            var a3 = new Contact("a") {Clock = 3};
            var a2 = new Contact("a") {Clock = 2};
            var a4 = new Contact("a") {Clock = 4};

            kBucket.Add(a3);
            kBucket.Add(a2);
            Assert.AreEqual(1, kBucket.Count);
            Assert.IsTrue(kBucket.TryGet(a3.Id, out var current));
            Assert.AreSame(a3, current);

            kBucket.Add(a4);
            Assert.AreEqual(1, kBucket.Count);
            Assert.IsTrue(kBucket.TryGet(a4.Id, out current));
            Assert.AreSame(a4, current);
        }
    }
}
