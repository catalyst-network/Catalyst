using System.Linq;
using DnsClient.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catalyst.Helpers.Network.UnitTests
{
    [TestClass]
    public class DnsUnitTests
    {

        [TestMethod]
        public void MethodTest()
        {
            var dnsQueryResponse = Dns.GetTxtRecords("seed1.network.atlascity.io"); //@TODO test the list override method
            var answerSection = (TxtRecord) dnsQueryResponse.Answers.FirstOrDefault();
            var seedIp = answerSection.EscapedText.FirstOrDefault();
            Assert.AreEqual("92.207.178.198:42069", seedIp);
        }
    }
}