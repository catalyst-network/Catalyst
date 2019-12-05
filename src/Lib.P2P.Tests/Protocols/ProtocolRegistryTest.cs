using Lib.P2P.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.Protocols
{
    [TestClass]
    public class ProtocolRegistryTest
    {
        [TestMethod]
        public void PreRegistered()
        {
            CollectionAssert.Contains(ProtocolRegistry.Protocols.Keys, "/multistream/1.0.0");
            CollectionAssert.Contains(ProtocolRegistry.Protocols.Keys, "/plaintext/1.0.0");
        }
    }
}
