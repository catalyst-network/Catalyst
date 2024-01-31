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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Transports;
using MultiFormats;

namespace Lib.P2P.Tests.Transports
{
    public class TcpTest
    {
        [Test]
        public void Connect_Unknown_Port()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<SocketException>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.0.1/tcp/32700").Result;
            });
        }

        [Test]
        public void Connect_Missing_TCP_Port()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.0.1/udp/32700").Result;
            });
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.0.1").Result;
            });
        }

        [Test]
        public void Connect_Missing_IP_Address()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = tcp.ConnectAsync("/tcp/32700").Result;
            });
        }

        [Test]
        public void Connect_Unknown_Address()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<SocketException>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.10.10/tcp/32700").Result;
            });
        }

        [Test]
        public void Connect_Cancelled()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource();
            cs.Cancel();
            ExceptionAssert.Throws<OperationCanceledException>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.10.10/tcp/32700", cs.Token).Result;
            });
        }

        [Test]
        [Ignore("Inconsistent results")]
        public async Task TimeProtocol()
        {
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var server = await new MultiAddress("/dns4/time.nist.gov/tcp/37").ResolveAsync(cs.Token);
            var data = new byte[4];

            var tcp = new Tcp();
            await using (var time = await tcp.ConnectAsync(server[0], cs.Token))
            {
                var n = await time.ReadAsync(data, 0, data.Length, cs.Token);
                Assert.Equals(4, n); // sometimes zero!
            }
        }

        [Test]
        public void Listen_Then_Cancel()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource();

            void Handler(Stream stream, MultiAddress local, MultiAddress remote)
            {
                Assert.Fail("handler should not be called");
            }
            
            var listenerAddress = tcp.Listen("/ip4/127.0.0.1", Handler, cs.Token);
            Assert.That(listenerAddress.Protocols.Any(p => p.Name == "tcp"), Is.True);
            cs.Cancel();
        }

        [Test]
        public async Task Listen()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var connected = false;
            MultiAddress listenerAddress = null;

            void Handler(Stream stream, MultiAddress local, MultiAddress remote)
            {
                Assert.That(stream, Is.Not.Null);
                Assert.That(listenerAddress, Is.EqualTo(local));
                Assert.That(remote, Is.Not.Null);
                Assert.That(local, Is.Not.EqualTo(remote));
                connected = true;
            }

            try
            {
                listenerAddress = tcp.Listen("/ip4/127.0.0.1", Handler, cs.Token);
                Assert.That(listenerAddress.Protocols.Any(p => p.Name == "tcp"), Is.True);
                await using (var stream = await tcp.ConnectAsync(listenerAddress, cs.Token))
                {
                    await Task.Delay(50, cs.Token);
                    Assert.That(stream, Is.Not.Null);
                    Assert.That(connected, Is.True);
                }
            }
            finally
            {
                cs.Cancel();
            }
        }

        [Test]
        public async Task Listen_Handler_Throws()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var called = false;

            void Handler(Stream stream, MultiAddress local, MultiAddress remote)
            {
                called = true;
                throw new Exception("foobar");
            }

            try
            {
                var addr = tcp.Listen("/ip4/127.0.0.1", Handler, cs.Token);
                Assert.That(addr.Protocols.Any(p => p.Name == "tcp"), Is.True);
                await using (var stream = await tcp.ConnectAsync(addr, cs.Token))
                {
                    await Task.Delay(50, cs.Token);
                    Assert.That(stream, Is.Not.Null);
                    Assert.That(called, Is.True);
                }
            }
            finally
            {
                cs.Cancel();
            }
        }

        [Test]
        public async Task SendReceive()
        {
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var tcp = new Tcp();
            
            using (var server = new HelloServer())
            {
                await using (var stream = await tcp.ConnectAsync(server.Address, cs.Token))
                {
                    var bytes = new byte[5];
                    await stream.ReadAsync(bytes, 0, bytes.Length, cs.Token);
                    Assert.That("hello", Is.EqualTo(Encoding.UTF8.GetString(bytes)));
                }
            }
        }

        private sealed class HelloServer : IDisposable
        {
            private readonly CancellationTokenSource _cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            public HelloServer()
            {
                var tcp = new Tcp();
                Address = tcp.Listen("/ip4/127.0.0.1", Handler, _cs.Token);
            }

            public MultiAddress Address { get; }

            public void Dispose() { _cs.Cancel(); }

            private static void Handler(Stream stream, MultiAddress local, MultiAddress remote)
            {
                var msg = Encoding.UTF8.GetBytes("hello");
                stream.Write(msg, 0, msg.Length);
                stream.Flush();
                stream.Dispose();
            }
        }
    }
}
