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
            
            
        }
    }
}