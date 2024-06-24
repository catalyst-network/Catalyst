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

namespace Lib.P2P.Tests.Protocols
{
    public class VersionedNameTest
    {
        [Test]
        public void Parsing()
        {
            var vn = VersionedName.Parse("/multistream/1.0.0");
            Assert.That(vn.Name, Is.EqualTo("multistream"));
            Assert.That(vn.Version.ToString(), Is.EqualTo("1.0.0"));

            vn = VersionedName.Parse("/ipfs/id/1.0.0");
            Assert.That(vn.Name, Is.EqualTo("ipfs/id"));
            Assert.That(vn.Version.ToString(), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void Stringing()
        {
            var vn = new VersionedName {Name = "x", Version = new Semver.SemVersion(0, 42)};
            Assert.That(vn.ToString(), Is.EqualTo("/x/0.42.0"));
        }

        [Test]
        public void Value_Equality()
        {
            var a0 = VersionedName.Parse("/x/1.0.0");
            var a1 = VersionedName.Parse("/x/1.0.0");
            var b = VersionedName.Parse("/x/1.1.0");
            VersionedName? c = null;
            VersionedName? d = null;

            Assert.That(c == d, Is.True);
            Assert.That(c == b, Is.False);
            Assert.That(b == c, Is.False);

            Assert.That(c != d, Is.False);
            Assert.That(c != b, Is.True);
            Assert.That(b != c, Is.True);

#pragma warning disable 1718
            Assert.That(a0 == a0, Is.True);
            Assert.That(a0 == a1, Is.True);
            Assert.That(a0 == b, Is.False);

#pragma warning disable 1718
            Assert.That(a0 != a0, Is.False);
            Assert.That(a0 != a1, Is.False);
            Assert.That(a0 != b, Is.True);

            Assert.That(a0.Equals(a0), Is.True);
            Assert.That(a0.Equals(a1), Is.True);
            Assert.That(a0.Equals(b), Is.False);

            Assert.That(a0, Is.EqualTo(a0));
            Assert.That(a0, Is.EqualTo(a1));
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.That(a0, Is.EqualTo(a0));
            Assert.That(a0, Is.EqualTo(a1));
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.That(a0.GetHashCode(), Is.EqualTo(a0.GetHashCode()));
            Assert.That(a1.GetHashCode(), Is.EqualTo(a0.GetHashCode()));
            Assert.That(a0.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Comparing()
        {
            var a0 = VersionedName.Parse("/x/1.0.0");
            var a1 = VersionedName.Parse("/x/1.0.0");
            var b = VersionedName.Parse("/x/1.1.0");
            var c = VersionedName.Parse("/y/0.42.0");

            Assert.That(a0.CompareTo(a1), Is.EqualTo(0));
            Assert.That(a1.CompareTo(a0), Is.EqualTo(0));

            Assert.That(b.CompareTo(a0), Is.EqualTo(1));
            Assert.That(a0.CompareTo(b), Is.EqualTo(-1));

            Assert.That(c.CompareTo(b), Is.EqualTo(1));
            Assert.That(b.CompareTo(c), Is.EqualTo(-1));
        }

        [Test]
        public void Ordering()
        {
            var names = new List<VersionedName>
            {
                VersionedName.Parse("/x/1.0.0"),
                VersionedName.Parse("/x/1.1.0"),
                VersionedName.Parse("/y/0.42.0"),
            };
            var ordered = names.OrderByDescending(n => n).ToArray();
            Assert.That(ordered[0].ToString(), Is.EqualTo("/y/0.42.0"));
            Assert.That(ordered[1].ToString(), Is.EqualTo("/x/1.1.0"));
            Assert.That(ordered[2].ToString(), Is.EqualTo("/x/1.0.0"));
        }
    }
}
