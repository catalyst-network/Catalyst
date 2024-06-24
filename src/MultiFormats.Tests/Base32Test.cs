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

using System.Text;

namespace MultiFormats.Tests
{
    public class Base32EncodeTests
    {
        private byte[] GetStringBytes(string x) { return Encoding.ASCII.GetBytes(x); }

        [Test]
        public void Vector1() { Assert.That(Base32.Encode(GetStringBytes(string.Empty)), Is.EqualTo(string.Empty)); }

        [Test]
        public void Vector2() { Assert.That(Base32.Encode(GetStringBytes("f")), Is.EqualTo("my")); }

        [Test]
        public void Vector3() { Assert.That(Base32.Encode(GetStringBytes("fo")), Is.EqualTo("mzxq")); }

        [Test]
        public void Vector4() { Assert.That(Base32.Encode(GetStringBytes("foo")), Is.EqualTo("mzxw6")); }

        [Test]
        public void Vector5() { Assert.That(Base32.Encode(GetStringBytes("foob")), Is.EqualTo("mzxw6yq")); }

        [Test]
        public void Vector6() { Assert.That(Base32.Encode(GetStringBytes("fooba")), Is.EqualTo("mzxw6ytb")); }

        [Test]
        public void Vector7() { Assert.That(Base32.Encode(GetStringBytes("foobar")), Is.EqualTo("mzxw6ytboi")); }
    }

    public class Base32DecodeTests
    {
        private byte[] GetStringBytes(string x) { return Encoding.ASCII.GetBytes(x); }

        [Test]
        public void Vector1() { Assert.That(GetStringBytes(string.Empty), Is.EquivalentTo(Base32.Decode(string.Empty))); }

        [Test]
        public void Vector2() { Assert.That(GetStringBytes("f"), Is.EquivalentTo(Base32.Decode("MY======"))); }

        [Test]
        public void Vector3() { Assert.That(GetStringBytes("fo"), Is.EquivalentTo(Base32.Decode("MZXQ===="))); }

        [Test]
        public void Vector4() { Assert.That(GetStringBytes("foo"), Is.EquivalentTo(Base32.Decode("MZXW6==="))); }

        [Test]
        public void Vector5() { Assert.That(GetStringBytes("foob"), Is.EquivalentTo(Base32.Decode("MZXW6YQ="))); }

        [Test]
        public void Vector6() { Assert.That(GetStringBytes("fooba"), Is.EquivalentTo(Base32.Decode("MZXW6YTB"))); }

        [Test]
        public void Vector7()
        {
            Assert.That(GetStringBytes("foobar"), Is.EquivalentTo(Base32.Decode("MZXW6YTBOI======")));
        }
    }
}
