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
using System.Threading.Tasks;
using Lib.P2P.Multiplex;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats;

namespace Lib.P2P.Tests.Multiplex
{
    [TestClass]
    public class SubstreamTest
    {
        [TestMethod]
        public void Seeking()
        {
            var stream = new Substream();
            Assert.IsFalse(stream.CanSeek);
            ExceptionAssert.Throws<NotSupportedException>(() => { stream.Seek(0, SeekOrigin.Begin); });
            ExceptionAssert.Throws<NotSupportedException>(() => { stream.Position = 0; });
            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                var _ = stream.Position;
            });
        }

        [TestMethod]
        public void Timeout()
        {
            var stream = new Substream();
            Assert.IsFalse(stream.CanTimeout);
            ExceptionAssert.Throws<InvalidOperationException>(() => { stream.ReadTimeout = 0; });
            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                var _ = stream.ReadTimeout;
            });
            ExceptionAssert.Throws<InvalidOperationException>(() => { stream.WriteTimeout = 0; });
            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                var _ = stream.WriteTimeout;
            });
        }

        [TestMethod]
        public void Length()
        {
            var stream = new Substream();
            ExceptionAssert.Throws<NotSupportedException>(() => { stream.SetLength(0); });
            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                var _ = stream.Length;
            });
        }

        [TestMethod]
        public async Task Reading()
        {
            var m1 = new byte[] {1, 2, 3, 4};
            var m2 = new byte[m1.Length];
            var stream = new Substream();
            stream.AddData(new byte[] {1, 2});
            stream.AddData(new byte[] {3, 4});
            stream.NoMoreData();
            Assert.IsTrue(stream.CanRead);

            m2[0] = (byte) stream.ReadByte();
            Assert.AreEqual(1, stream.Read(m2, 1, 1));
            Assert.AreEqual(2, await stream.ReadAsync(m2, 2, 2));
            CollectionAssert.AreEqual(m1, m2);

            Assert.AreEqual(-1, stream.ReadByte());
            Assert.IsFalse(stream.CanRead);
        }

        [TestMethod]
        public async Task Reading_Partial()
        {
            var m1 = new byte[] {1, 2, 3, 4};
            var m2 = new byte[m1.Length];
            var stream = new Substream();
            stream.AddData(m1);
            stream.NoMoreData();

            Assert.AreEqual(4, await stream.ReadAsync(m2, 0, 5));
            CollectionAssert.AreEqual(m1, m2);

            Assert.AreEqual(-1, stream.ReadByte());
            Assert.IsFalse(stream.CanRead);
        }

        [TestMethod]
        public async Task Reading_Delayed_Partial()
        {
            var m1 = new byte[] {1, 2, 3, 4};
            var m2 = new byte[m1.Length];
            var stream = new Substream();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                await Task.Delay(100);
                stream.AddData(new byte[] {1, 2});
                await Task.Delay(100);
                stream.AddData(new byte[] {3, 4});
                await Task.Delay(100);
                stream.NoMoreData();
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            Assert.AreEqual(4, await stream.ReadAsync(m2, 0, 5));
            CollectionAssert.AreEqual(m1, m2);
        }

        [TestMethod]
        public void Reading_Empty()
        {
            var m1 = new byte[0];
            var stream = new Substream();
            var _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                stream.NoMoreData();
            });

            Assert.AreEqual(-1, stream.ReadByte());
        }

        [TestMethod]
        public async Task Reading_ClosedStream()
        {
            var m1 = new byte[10];
            var stream = new Substream();
            stream.NoMoreData();
            Assert.AreEqual(0, await stream.ReadAsync(m1, 0, 10));
        }

        [TestMethod]
        public async Task Writing()
        {
            var ms = new MemoryStream();
            var muxer = new Muxer {Channel = ms};
            var stream = new Substream {Muxer = muxer};
            var m1 = new byte[1];
            stream.AddData(new byte[] {10});
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanWrite);

            Assert.AreEqual(1, await stream.ReadAsync(m1, 0, 1));
            await stream.WriteAsync(m1, 0, 1);
            stream.WriteByte(11);
            await stream.FlushAsync();

            ms.Position = 0;
            var header = await Header.ReadAsync(ms);
            var length = await Varint.ReadVarint32Async(ms);
            var payload = new byte[length];
            ms.Read(payload, 0, length);
            Assert.AreEqual(stream.Id, header.StreamId);
            Assert.AreEqual(2, payload.Length);
            CollectionAssert.AreEqual(new byte[] {10, 11}, payload);
        }

        [TestMethod]
        public void Disposable()
        {
            var s = new Substream();
            Assert.IsTrue(s.CanRead);
            Assert.IsTrue(s.CanWrite);

            s.Dispose();
            Assert.IsFalse(s.CanRead);
            Assert.IsFalse(s.CanWrite);
        }
    }
}
