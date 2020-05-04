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
        private Socket _socket;
        private bool _ownsSocket;
        private MemoryStream _sendBuffer = new MemoryStream();
        private MemoryStream _receiveBuffer = new MemoryStream();
        private byte[] _datagram = new byte[2048];

        public DatagramStream(Socket socket, bool ownsSocket = false)
        {
            this._socket = socket;
            this._ownsSocket = ownsSocket;
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

            if (_ownsSocket && _socket != null)
                try
                {
                    _socket.Dispose();
                }
                catch (SocketException)
                {
                    // eat it
                }
                finally
                {
                    _socket = null;
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
            if (_sendBuffer.Position > 0)
            {
                var bytes = new ArraySegment<byte>(_sendBuffer.ToArray());
                _sendBuffer.Position = 0;
                await _socket.SendAsync(bytes, SocketFlags.None).ConfigureAwait(false);
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
            if (_receiveBuffer.Position != _receiveBuffer.Length)
            {
                return _receiveBuffer.Read(buffer, offset, count);
            }
            
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            _receiveBuffer.Position = 0;
            _receiveBuffer.SetLength(0);
            var size = _socket.Receive(_datagram);
            await _receiveBuffer.WriteAsync(_datagram, 0, size, cancellationToken);
            _receiveBuffer.Position = 0;

            return _receiveBuffer.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

        public override void SetLength(long value) { throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count) { _sendBuffer.Write(buffer, offset, count); }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }
    }
}
