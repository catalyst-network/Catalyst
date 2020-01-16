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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P.Transports
{
    internal class DatagramStream : Stream
    {
        private Socket socket;
        private bool ownsSocket;
        private MemoryStream sendBuffer = new MemoryStream();
        private MemoryStream receiveBuffer = new MemoryStream();
        private byte[] datagram = new byte[2048];

        public DatagramStream(Socket socket, bool ownsSocket = false)
        {
            this.socket = socket;
            this.ownsSocket = ownsSocket;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                try
                {
                    Flush();
                }
                catch (SocketException)
                {
                    // eat it
                }

            if (ownsSocket && socket != null)
                try
                {
                    socket.Dispose();
                }
                catch (SocketException)
                {
                    // eat it
                }
                finally
                {
                    socket = null;
                }

            base.Dispose(disposing);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
#pragma warning disable VSTHRD002 
            FlushAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (sendBuffer.Position > 0)
            {
                var bytes = new ArraySegment<byte>(sendBuffer.ToArray());
                sendBuffer.Position = 0;
                await socket.SendAsync(bytes, SocketFlags.None).ConfigureAwait(false);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
#pragma warning disable VSTHRD002 
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 
        }

        public override async Task<int> ReadAsync(byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            // If no data.
            if (receiveBuffer.Position == receiveBuffer.Length)
            {
                await FlushAsync().ConfigureAwait(false);
                receiveBuffer.Position = 0;
                receiveBuffer.SetLength(0);
                var size = socket.Receive(datagram);
                await receiveBuffer.WriteAsync(datagram, 0, size);
                receiveBuffer.Position = 0;
            }

            return receiveBuffer.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

        public override void SetLength(long value) { throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count) { sendBuffer.Write(buffer, offset, count); }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }
    }
}
