using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.Helpers.Network.UnitTests
{
    [TestClass]
    public class DnsUnitTests
    {

        [TestMethod]
        public void MethodTest()
        {
            var seedIp = Dns.GetTxtRecords("seed1.network.atlascity.io");
            
            foreach (var ip in seedIp)
            {
                Assert.AreEqual("92.207.178.198:42069", ip);
            }
        }
    }
}