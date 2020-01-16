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

// BufferedStream is not available in Net Stardard 1.4

using System.Threading;
using System.Threading.Tasks;
#if !NETSTANDARD14

// Part of JuiceStream: https://juicestream.machinezoo.com
using System;
using System.IO;

namespace Lib.P2P
{
    /// <summary>
    /// .NET already has its <c>BufferedStream</c>, but that one will throw unexpected exceptions, especially on <c>NetworkStreams</c>.
    /// JuiceStream's <c>DuplexBufferedStream</c> embeds two <c>BufferedStream</c> instances,
    /// one for each direction, to provide full duplex buffering over non-seekable streams.
    /// </summary>
    /// <remarks>
    ///   Copied from <see href="https://bitbucket.org/robertvazan/juicestream/raw/2caa975524900d1b5a76ddd3731c273d5dbb51eb/JuiceStream/DuplexBufferedStream.cs"/>
    /// </remarks>
    internal class DuplexBufferedStream : Stream
    {
        private readonly Stream Inner;
        private readonly BufferedStream ReadBuffer;
        private readonly BufferedStream WriteBuffer;

        public override bool CanRead => Inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => Inner.CanWrite;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public DuplexBufferedStream(Stream stream)
        {
            Inner = stream;
            ReadBuffer = new BufferedStream(stream);
            WriteBuffer = new BufferedStream(stream);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WriteBuffer.Flush();
                Inner.Dispose();
                ReadBuffer.Dispose();
                WriteBuffer.Dispose();
            }
        }

        public override void Flush() { WriteBuffer.Flush(); }
        public override Task FlushAsync(CancellationToken token) { return WriteBuffer.FlushAsync(token); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadBuffer.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return ReadBuffer.ReadAsync(buffer, offset, count, token);
        }

        public override int ReadByte() { return ReadBuffer.ReadByte(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { WriteBuffer.Write(buffer, offset, count); }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return WriteBuffer.WriteAsync(buffer, offset, count, token);
        }

        public override void WriteByte(byte value) { WriteBuffer.WriteByte(value); }
    }
}

#endif
