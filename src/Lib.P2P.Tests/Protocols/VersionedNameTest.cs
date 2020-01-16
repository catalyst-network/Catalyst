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

using System.Collections.Generic;
using System.Linq;
using Lib.P2P.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lib.P2P.Tests.Protocols
{
    [TestClass]
    public class VersionedNameTest
    {
        [TestMethod]
        public void Parsing()
        {
            var vn = VersionedName.Parse("/multistream/1.0.0");
            Assert.AreEqual("multistream", vn.Name);
            Assert.AreEqual("1.0.0", vn.Version.ToString());

            vn = VersionedName.Parse("/ipfs/id/1.0.0");
            Assert.AreEqual("ipfs/id", vn.Name);
            Assert.AreEqual("1.0.0", vn.Version.ToString());
        }

        [TestMethod]
        public void Stringing()
        {
            var vn = new VersionedName {Name = "x", Version = new Semver.SemVersion(0, 42)};
            Assert.AreEqual("/x/0.42.0", vn.ToString());
        }

        [TestMethod]
        public void Value_Equality()
        {
            var a0 = VersionedName.Parse("/x/1.0.0");
            var a1 = VersionedName.Parse("/x/1.0.0");
            var b = VersionedName.Parse("/x/1.1.0");
            VersionedName c = null;
            VersionedName d = null;

            Assert.IsTrue(c == d);
            Assert.IsFalse(c == b);
            Assert.IsFalse(b == c);

            Assert.IsFalse(c != d);
            Assert.IsTrue(c != b);
            Assert.IsTrue(b != c);

#pragma warning disable 1718
            Assert.IsTrue(a0 == a0);
            Assert.IsTrue(a0 == a1);
            Assert.IsFalse(a0 == b);

#pragma warning disable 1718
            Assert.IsFalse(a0 != a0);
            Assert.IsFalse(a0 != a1);
            Assert.IsTrue(a0 != b);

            Assert.IsTrue(a0.Equals(a0));
            Assert.IsTrue(a0.Equals(a1));
            Assert.IsFalse(a0.Equals(b));

            Assert.AreEqual(a0, a0);
            Assert.AreEqual(a0, a1);
            Assert.AreNotEqual(a0, b);

            Assert.AreEqual<VersionedName>(a0, a0);
            Assert.AreEqual<VersionedName>(a0, a1);
            Assert.AreNotEqual<VersionedName>(a0, b);

            Assert.AreEqual(a0.GetHashCode(), a0.GetHashCode());
            Assert.AreEqual(a0.GetHashCode(), a1.GetHashCode());
            Assert.AreNotEqual(a0.GetHashCode(), b.GetHashCode());
        }

        [TestMethod]
        public void Comparing()
        {
            var a0 = VersionedName.Parse("/x/1.0.0");
            var a1 = VersionedName.Parse("/x/1.0.0");
            var b = VersionedName.Parse("/x/1.1.0");
            var c = VersionedName.Parse("/y/0.42.0");

            Assert.AreEqual(0, a0.CompareTo(a1));
            Assert.AreEqual(0, a1.CompareTo(a0));

            Assert.AreEqual(1, b.CompareTo(a0));
            Assert.AreEqual(-1, a0.CompareTo(b));

            Assert.AreEqual(1, c.CompareTo(b));
            Assert.AreEqual(-1, b.CompareTo(c));
        }

        [TestMethod]
        public void Ordering()
        {
            var names = new List<VersionedName>
            {
                VersionedName.Parse("/x/1.0.0"),
                VersionedName.Parse("/x/1.1.0"),
                VersionedName.Parse("/y/0.42.0"),
            };
            var ordered = names.OrderByDescending(n => n).ToArray();
            Assert.AreEqual("/y/0.42.0", ordered[0].ToString());
            Assert.AreEqual("/x/1.1.0", ordered[1].ToString());
            Assert.AreEqual("/x/1.0.0", ordered[2].ToString());
        }
    }
}
