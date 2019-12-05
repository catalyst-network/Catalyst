﻿using Lib.P2P.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.Cryptography
{
    [TestClass]
    public class EphermalKeyTest
    {
        [TestMethod]
        public void SharedSecret()
        {
            var curve = "P-256";
            var alice = EphermalKey.Generate(curve);
            var bob = EphermalKey.Generate(curve);

            var aliceSecret = alice.GenerateSharedSecret(bob);
            var bobSecret = bob.GenerateSharedSecret(alice);
            CollectionAssert.AreEqual(aliceSecret, bobSecret);
            Assert.AreEqual(32, aliceSecret.Length);
        }
    }
}
