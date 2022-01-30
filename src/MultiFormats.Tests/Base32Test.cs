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

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiFormats.Tests
{
    [TestClass]
    public class Base32EncodeTests
    {
        private byte[] GetStringBytes(string x) { return Encoding.ASCII.GetBytes(x); }

        [TestMethod]
        public void Vector1() { Assert.AreEqual(string.Empty, Base32.Encode(GetStringBytes(string.Empty))); }

        [TestMethod]
        public void Vector2() { Assert.AreEqual("my", Base32.Encode(GetStringBytes("f"))); }

        [TestMethod]
        public void Vector3() { Assert.AreEqual("mzxq", Base32.Encode(GetStringBytes("fo"))); }

        [TestMethod]
        public void Vector4() { Assert.AreEqual("mzxw6", Base32.Encode(GetStringBytes("foo"))); }

        [TestMethod]
        public void Vector5() { Assert.AreEqual("mzxw6yq", Base32.Encode(GetStringBytes("foob"))); }

        [TestMethod]
        public void Vector6() { Assert.AreEqual("mzxw6ytb", Base32.Encode(GetStringBytes("fooba"))); }

        [TestMethod]
        public void Vector7() { Assert.AreEqual("mzxw6ytboi", Base32.Encode(GetStringBytes("foobar"))); }
    }

    [TestClass]
    public class Base32DecodeTests
    {
        private byte[] GetStringBytes(string x) { return Encoding.ASCII.GetBytes(x); }

        [TestMethod]
        public void Vector1() { CollectionAssert.AreEqual(GetStringBytes(string.Empty), Base32.Decode(string.Empty)); }

        [TestMethod]
        public void Vector2() { CollectionAssert.AreEqual(GetStringBytes("f"), Base32.Decode("MY======")); }

        [TestMethod]
        public void Vector3() { CollectionAssert.AreEqual(GetStringBytes("fo"), Base32.Decode("MZXQ====")); }

        [TestMethod]
        public void Vector4() { CollectionAssert.AreEqual(GetStringBytes("foo"), Base32.Decode("MZXW6===")); }

        [TestMethod]
        public void Vector5() { CollectionAssert.AreEqual(GetStringBytes("foob"), Base32.Decode("MZXW6YQ=")); }

        [TestMethod]
        public void Vector6() { CollectionAssert.AreEqual(GetStringBytes("fooba"), Base32.Decode("MZXW6YTB")); }

        [TestMethod]
        public void Vector7()
        {
            CollectionAssert.AreEqual(GetStringBytes("foobar"), Base32.Decode("MZXW6YTBOI======"));
        }
    }
}
