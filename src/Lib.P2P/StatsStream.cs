#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Lib.P2P.Transports;

namespace Lib.P2P
{
    /// <summary>
    ///   A simple wrapper around another stream that records statistics.
    /// </summary>
    public sealed class StatsStream : Stream
    {
        /// <summary>
        ///   A summary of all StatStreams.
        /// </summary>
        public static readonly BandwidthData AllBandwidth = new BandwidthData
        {
            RateIn = 5 * 1024,
            RateOut = 1024
        };

        private Stream _stream;
        private long _bytesRead;
        private long _bytesWritten;
        private DateTime _lastUsed;

        static StatsStream()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    lock (AllBandwidth)
                    {
                        AllBandwidth.RateIn = 0;
                        AllBandwidth.RateOut = 0;
                    }
                }
            });
        }

        /// <summary>
        ///   Create a <see cref="StatsStream"/> for the specified stream.
        /// </summary>
        internal StatsStream(Stream stream) { this._stream = stream; }

        /// <summary>
        ///   Total number of bytes read on the stream.
        /// </summary>
        internal long BytesRead => _bytesRead;

        /// <summary>
        ///   Total number of byte written to the stream.
        /// </summary>
        internal long BytesWritten => _bytesWritten;

        /// <summary>
        ///   The last time a write or read occured.
        /// </summary>
        internal DateTime LastUsed => _lastUsed;

        /// <inheritdoc />
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _stream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => _stream.CanWrite;

        /// <inheritdoc />
        public override long Length => _stream.Length;

        /// <inheritdoc />
        public override bool CanTimeout => _stream.CanTimeout;

        /// <inheritdoc />
        public override int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }

        /// <inheritdoc />
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        /// <inheritdoc />
        public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }

        /// <inheritdoc />
        public override void Flush() { _stream.Flush(); }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _stream.Read(buffer, offset, count);
            _bytesRead += n;
            _lastUsed = DateTime.Now;
            if (n <= 0)
            {
                return n;
            }
            
            AllBandwidth.TotalIn += (ulong) n;
            AllBandwidth.RateIn += n;

            return n;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) { return _stream.Seek(offset, origin); }

        /// <inheritdoc />
        public override void SetLength(long value) { _stream.SetLength(value); }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            _bytesWritten += count;
            _lastUsed = DateTime.Now;
            
            if (count <= 0)
            {
                return;
            }
            
            AllBandwidth.TotalOut += (ulong) count;
            AllBandwidth.RateOut += count;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            try
            {
                var n = await _stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                _bytesRead += n;
                _lastUsed = DateTime.Now;
                if (n <= 0)
                {
                    return n;
                }
                
                AllBandwidth.TotalIn += (ulong) n;
                AllBandwidth.RateIn += n;

                return n;
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                await _stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                _bytesWritten += count;
                _lastUsed = DateTime.Now;
                if (count > 0)

                    //lock (AllBandwidth)
                {
                    AllBandwidth.TotalOut += (ulong) count;
                    AllBandwidth.RateOut += count;
                }
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // eat it.
            }
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            var n = _stream.ReadByte();
            if (n > -1)
            {
                ++_bytesRead;

                //lock (AllBandwidth)
                {
                    ++AllBandwidth.TotalIn;
                    ++AllBandwidth.RateIn;
                }
            }

            _lastUsed = DateTime.Now;
            return n;
        }

        /// <inheritdoc />
        public override void WriteByte(byte value)
        {
            _stream.WriteByte(value);
            ++_bytesWritten;
            _lastUsed = DateTime.Now;

            //lock (AllBandwidth)
            {
                ++AllBandwidth.TotalOut;
                ++AllBandwidth.RateOut;
            }
        }
    }
}
