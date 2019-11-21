using System.Security;
using Ipfs.Core.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Core.Tests.Cryptography
{
    [TestClass]
    public class SecureStringExtensionsTest
    {
        [TestMethod]
        public void UseBytes()
        {
            var secret = new SecureString();
            var expected = new char[] {'a', 'b', 'c'};
            foreach (var c in expected) secret.AppendChar(c);
            secret.UseSecretBytes(bytes =>
            {
                Assert.AreEqual<int>(expected.Length, bytes.Length);
                for (var i = 0; i < expected.Length; ++i)
                    Assert.AreEqual((int) expected[i], (int) bytes[i]);
            });
        }
    }
}
