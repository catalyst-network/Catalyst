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
using System.IO;
using System.Linq;

namespace MultiFormats.Tests
{
    public sealed class HexStringTest
    {
        [Test]
        public void Encode()
        {
            var buffer = Enumerable.Range(byte.MinValue, byte.MaxValue).Select(b => (byte)b).ToArray();
            var lowerHex = string.Concat(buffer.Select(b => b.ToString("x2")).ToArray());
            var upperHex = string.Concat(buffer.Select(b => b.ToString("X2")).ToArray());

            Assert.That(lowerHex, Is.EqualTo(buffer.ToHexString()), "encode default");
            Assert.That(lowerHex, Is.EqualTo(buffer.ToHexString()), "encode general");
            Assert.That(lowerHex, Is.EqualTo(buffer.ToHexString("x")), "encode lower");
            Assert.That(upperHex, Is.EqualTo(buffer.ToHexString("X")), "encode upper");
        }

        [Test]
        public void Decode()
        {
            var buffer = Enumerable.Range(byte.MinValue, byte.MaxValue).Select(b => (byte)b).ToArray();
            var lowerHex = string.Concat(buffer.Select(b => b.ToString("x2")).ToArray());
            var upperHex = string.Concat(buffer.Select(b => b.ToString("X2")).ToArray());

            Assert.That(buffer, Is.EqualTo(lowerHex.ToHexBuffer()), "decode lower");
            Assert.That(buffer, Is.EqualTo(upperHex.ToHexBuffer()), "decode upper");
        }

        [Test]
        public void InvalidFormatSpecifier()
        {
            ExceptionAssert.Throws<FormatException>(() => HexString.Encode(new byte[0], "..."));
        }

        [Test]
        public void InvalidHexStrings()
        {
            ExceptionAssert.Throws<InvalidDataException>(() => HexString.Decode("0"));
            ExceptionAssert.Throws<InvalidDataException>(() => HexString.Decode("0Z"));
        }
    }
}
