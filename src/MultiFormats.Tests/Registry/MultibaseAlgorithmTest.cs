#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats.Registry;

namespace MultiFormats.Tests.Registry
{
    [TestClass]
    public class MultiBaseAlgorithmTest
    {
        [TestMethod]
        public void Bad_Name()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => MultiBaseAlgorithm.Register(null, '?'));
            ExceptionAssert.Throws<ArgumentNullException>(() => MultiBaseAlgorithm.Register("", '?'));
            ExceptionAssert.Throws<ArgumentNullException>(() => MultiBaseAlgorithm.Register("   ", '?'));
        }

        [TestMethod]
        public void Name_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => MultiBaseAlgorithm.Register("base58btc", 'z'));
        }

        [TestMethod]
        public void Code_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => MultiBaseAlgorithm.Register("base58btc-x", 'z'));
        }

        [TestMethod]
        public void Algorithms_Are_Enumerable() { Assert.AreNotEqual(0, MultiBaseAlgorithm.All.Count()); }

        [TestMethod]
        public void Roundtrip_All_Algorithms()
        {
            var bytes = new byte[]
            {
                1, 2, 3, 4, 5
            };

            foreach (var alg in MultiBaseAlgorithm.All)
            {
                var s = alg.Encode(bytes);
                CollectionAssert.AreEqual(bytes, alg.Decode(s), alg.Name);
            }
        }

        [TestMethod]
        public void Name_Is_Also_ToString()
        {
            foreach (var alg in MultiBaseAlgorithm.All) Assert.AreEqual(alg.Name, alg.ToString());
        }

        [TestMethod]
        public void Known_But_NYI()
        {
            var alg = MultiBaseAlgorithm.Register("nyi", 'n');
            try
            {
                ExceptionAssert.Throws<NotImplementedException>(() => alg.Encode(null));
                ExceptionAssert.Throws<NotImplementedException>(() => alg.Decode(null));
            }
            finally
            {
                MultiBaseAlgorithm.Deregister(alg);
            }
        }
    }
}
