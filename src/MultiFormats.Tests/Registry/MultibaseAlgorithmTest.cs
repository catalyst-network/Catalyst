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

using System;
using System.Linq;
using MultiFormats.Registry;

namespace MultiFormats.Tests.Registry
{
    public class MultiBaseAlgorithmTest
    {
        [Test]
        public void Bad_Name()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => MultiBaseAlgorithm.Register(null, '?'));
            ExceptionAssert.Throws<ArgumentNullException>(() => MultiBaseAlgorithm.Register("", '?'));
            ExceptionAssert.Throws<ArgumentNullException>(() => MultiBaseAlgorithm.Register("   ", '?'));
        }

        [Test]
        public void Name_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => MultiBaseAlgorithm.Register("base58btc", 'z'));
        }

        [Test]
        public void Code_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => MultiBaseAlgorithm.Register("base58btc-x", 'z'));
        }

        [Test]
        public void Algorithms_Are_Enumerable() { Assert.That(0, Is.Not.EqualTo(MultiBaseAlgorithm.All.Count())); }

        [Test]
        public void Roundtrip_All_Algorithms()
        {
            var bytes = new byte[]
            {
                1, 2, 3, 4, 5
            };

            foreach (var alg in MultiBaseAlgorithm.All)
            {
                var s = alg.Encode(bytes);
                Assert.That(bytes, Is.EquivalentTo(alg.Decode(s)), alg.Name);
            }
        }

        [Test]
        public void Name_Is_Also_ToString()
        {
            foreach (var alg in MultiBaseAlgorithm.All) Assert.That(alg.Name, Is.EqualTo(alg.ToString()));
        }

        [Test]
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
