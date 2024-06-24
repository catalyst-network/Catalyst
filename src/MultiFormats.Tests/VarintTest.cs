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
using System.Threading;
using System.Threading.Tasks;

namespace MultiFormats.Tests
{
    public class VarintTest
    {
        [Test]
        public void Zero()
        {
            var x = new byte[]
            {
                0
            };
            Assert.That(Varint.RequiredBytes(0), Is.EqualTo(1));
            Assert.That(x, Is.EquivalentTo(Varint.Encode(0)));
            Assert.That(Varint.DecodeInt32(x), Is.EqualTo(0));
        }

        [Test]
        public void ThreeHundred()
        {
            var x = new byte[]
            {
                0xAC, 0x02
            };
            Assert.That(Varint.RequiredBytes(300), Is.EqualTo(2));
            Assert.That(x, Is.EquivalentTo(Varint.Encode(300)));
            Assert.That(Varint.DecodeInt32(x), Is.EqualTo(300));
        }

        [Test]
        public void Decode_From_Offset()
        {
            var x = new byte[]
            {
                0x00, 0xAC, 0x02
            };
            Assert.That(Varint.DecodeInt32(x, 1), Is.EqualTo(300));
        }

        [Test]
        public void MaxLong()
        {
            var x = "ffffffffffffffff7f".ToHexBuffer();
            Assert.That(Varint.RequiredBytes(long.MaxValue), Is.EqualTo(9));
            Assert.That(x, Is.EquivalentTo(Varint.Encode(long.MaxValue)));
            Assert.That(Varint.DecodeInt64(x), Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void Encode_Negative() { ExceptionAssert.Throws<NotSupportedException>(() => Varint.Encode(-1)); }

        [Test]
        public void TooBig_Int32()
        {
            var bytes = Varint.Encode((long)int.MaxValue + 1);
            ExceptionAssert.Throws<InvalidDataException>(() => Varint.DecodeInt32(bytes));
        }

        [Test]
        public void TooBig_Int64()
        {
            var bytes = "ffffffffffffffffff7f".ToHexBuffer();
            ExceptionAssert.Throws<InvalidDataException>(() => Varint.DecodeInt64(bytes));
        }

        [Test]
        public void Unterminated()
        {
            var bytes = "ff".ToHexBuffer();
            ExceptionAssert.Throws<InvalidDataException>(() => Varint.DecodeInt64(bytes));
        }

        [Test]
        public void Empty()
        {
            var bytes = new byte[0];
            ExceptionAssert.Throws<EndOfStreamException>(() => Varint.DecodeInt64(bytes));
        }

        [Test]
        public async Task WriteAsync()
        {
            await using (var ms = new MemoryStream())
            {
                await ms.WriteVarintAsync(long.MaxValue);
                ms.Position = 0;
                Assert.That(ms.ReadVarint64(), Is.EqualTo(long.MaxValue));
            }
        }

        [Test]
        public void WriteAsync_Negative()
        {
            var ms = new MemoryStream();
            ExceptionAssert.Throws<Exception>(() => ms.WriteVarintAsync(-1).Wait());
        }

        [Test]
        public void WriteAsync_Cancel()
        {
            var ms = new MemoryStream();
            var cs = new CancellationTokenSource();
            cs.Cancel();
            ExceptionAssert.Throws<TaskCanceledException>(() => ms.WriteVarintAsync(0, cs.Token).Wait());
        }

        [Test]
        public async Task ReadAsync()
        {
            await using (var ms = new MemoryStream("ffffffffffffffff7f".ToHexBuffer()))
            {
                var v = await ms.ReadVarint64Async();
                Assert.That(v, Is.EqualTo(long.MaxValue));
            }
        }

        [Test]
        public void ReadAsync_Cancel()
        {
            var ms = new MemoryStream(new byte[]
            {
                0
            });
            var cs = new CancellationTokenSource();
            cs.Cancel();
            ExceptionAssert.Throws<TaskCanceledException>(() => ms.ReadVarint32Async(cs.Token).Wait());
        }

        [Test]
        public void Example()
        {
            for (long v = 1; v <= 0xFFFFFFFL; v = v << 4)
            {
                Console.Write($"| {v} (0x{v:x}) ");
                Console.WriteLine($"| {Varint.Encode(v).ToHexString()} |");
            }
        }
    }
}
