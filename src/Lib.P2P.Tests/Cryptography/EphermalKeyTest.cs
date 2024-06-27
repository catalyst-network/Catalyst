#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using Lib.P2P.Cryptography;
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
