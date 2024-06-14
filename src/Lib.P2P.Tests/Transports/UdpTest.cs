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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Transports;
using MultiFormats;

namespace Lib.P2P.Tests.Transports
{
    public class UdpTest
    {
        [Test]
        public void Connect_Missing_UDP_Port()
        {
            var udp = new Udp();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = udp.ConnectAsync("/ip4/127.0.0.1/tcp/32700").Result;
            });
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = udp.ConnectAsync("/ip4/127.0.0.1").Result;
            });
        }

        [Test]
        public void Connect_Missing_IP_Address()
        {
            var udp = new Udp();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = udp.ConnectAsync("/udp/32700").Result;
            });
        }

        [Test]
        public void Connect_Cancelled()
        {
            var udp = new Udp();
            var cs = new CancellationTokenSource();
            cs.Cancel();
            ExceptionAssert.Throws<OperationCanceledException>(() =>
            {
                var _ = udp.ConnectAsync("/ip4/127.0.10.10/udp/32700", cs.Token).Result;
            });
        }

        [Test]
        [Ignore("Pause")]
        public async Task Listen()
        {
            var udp = new Udp();
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var connected = false;

            void Handler(Stream stream, MultiAddress local, MultiAddress remote)
            {
                Assert.That(stream, Is.Not.Null);
                connected = true;
            }

            try
            {
                var listenerAddress = udp.Listen("/ip4/127.0.0.1", Handler, cs.Token);
                Assert.That(listenerAddress.Protocols.Any(p => p.Name == "udp"), Is.True);
                await using (var stream = await udp.ConnectAsync(listenerAddress, cs.Token))
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
        [Ignore("Sometimes fails")]
        public async Task NetworkTimeProtocol()
        {
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var server = await new MultiAddress("/dns4/time.windows.com/udp/123").ResolveAsync(cs.Token);
            var ntpData = new byte[48];
            ntpData[0] = 0x1B;

            var udp = new Udp();
            await using (var time = await udp.ConnectAsync(server[0], cs.Token))
            {
                ntpData[0] = 0x1B;
                await time.WriteAsync(ntpData, 0, ntpData.Length, cs.Token);
                await time.FlushAsync(cs.Token);
                await time.ReadAsync(ntpData, 0, ntpData.Length, cs.Token);
                Assert.That(ntpData[0], Is.Not.EqualTo(0x1B));

                Array.Clear(ntpData, 0, ntpData.Length);
                ntpData[0] = 0x1B;
                await time.WriteAsync(ntpData, 0, ntpData.Length, cs.Token);
                await time.FlushAsync(cs.Token);
                await time.ReadAsync(ntpData, 0, ntpData.Length, cs.Token);
                Assert.That(ntpData[0], Is.Not.EqualTo(0x1B));
            }
        }

        [Test]
        [Ignore("Pause")]
        public async Task SendReceive()
        {
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var udp = new Udp();
            
            using (var server = new HelloServer())
            {
                await using (var stream = await udp.ConnectAsync(server.Address, cs.Token))
                {
                    var bytes = new byte[5];
                    await stream.ReadAsync(bytes, 0, bytes.Length, cs.Token);
                    Assert.Equals("hello", Encoding.UTF8.GetString(bytes));
                }
            }
        }

        private sealed class HelloServer : IDisposable
        {
            private readonly CancellationTokenSource _cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            public HelloServer()
            {
                var udp = new Udp();
                Address = udp.Listen("/ip4/127.0.0.1", Handler, _cs.Token);
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
