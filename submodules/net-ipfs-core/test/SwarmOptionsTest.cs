using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Core.Tests
{
    [TestClass]
    public class SwarmOptionsTest
    {
        [TestMethod]
        public void Defaults()
        {
            var options = new SwarmOptions();
            Assert.IsNull(options.PrivateNetworkKey);
            Assert.AreEqual(8, options.MinConnections);
        }
    }
}
