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

namespace Catalyst.Core.Lib.IO
{
    /// <summary>
    ///   Provides read only access to a slice of stream.
    /// </summary>
    public sealed class SlicedStream : Stream
    {
        private Stream stream;
        private long offset;
        private long logicalEnd;

        public SlicedStream(Stream stream, long offset, long count)
        {
            this.stream = stream;
            this.offset = offset;

            stream.Position = offset;
            logicalEnd = count < 1
                ? stream.Length
                : Math.Min(stream.Length, offset + count);
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => stream.Length;

        public override bool CanTimeout => stream.CanTimeout;

        public override int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }

        public override int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        public override void Flush() { stream.Flush(); }

        public override long Position { get => stream.Position - offset; set => throw new NotSupportedException(); }

        public override int ReadByte()
        {
            if (stream.Position >= logicalEnd)
            {
                return -1;
            }
            
            return stream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (stream.Position >= logicalEnd)
                return 0;
            var length = Math.Min(count, logicalEnd - stream.Position);
            var n = stream.Read(buffer, offset, (int) length);
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

        public override void SetLength(long value) { throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return stream.FlushAsync(cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte value) { throw new NotSupportedException(); }
    }
}
