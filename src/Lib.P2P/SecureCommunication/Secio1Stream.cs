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
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Lib.P2P.SecureCommunication
{
    /// <summary>
    ///   A duplex stream that is encrypted and signed.
    /// </summary>
    /// <remarks>
    ///   A packet consists of a [uint32 length of packet | encrypted body | hmac signature of encrypted body].
    ///   <para>
    ///   Writing data is buffered until <see cref="FlushAsync(CancellationToken)"/> is
    ///   called.
    ///   </para>
    /// </remarks>
    public class Secio1Stream : Stream
    {
        private Stream _stream;
        private byte[] _inBlock;
        private int _inBlockOffset;
        private MemoryStream _outStream = new MemoryStream();
        private HMac _inHmac;
        private HMac _outHmac;
        private IStreamCipher _decrypt;
        private IStreamCipher _encrypt;

        /// <summary>
        ///   Creates a new instance of the <see cref="Secio1Stream"/> class. 
        /// </summary>
        /// <param name="stream">
        ///   The source/destination of SECIO packets.
        /// </param>
        /// <param name="cipherName">
        ///   The cipher for the <paramref name="stream"/>, such as AES-256 or AES-128.
        /// </param>
        /// <param name="hashName">
        ///   The hash for the <paramref name="stream"/>, such as SHA256.
        /// </param>
        /// <param name="localKey">
        ///   The keys used by the local endpoint.
        /// </param>
        /// <param name="remoteKey">
        ///   The keys used by the remote endpoint.
        /// </param>
        public Secio1Stream(Stream stream,
            string cipherName,
            string hashName,
            StretchedKey localKey,
            StretchedKey remoteKey)
        {
            this._stream = stream;

            _inHmac = new HMac(DigestUtilities.GetDigest(hashName));
            _inHmac.Init(new KeyParameter(localKey.MacKey));

            _outHmac = new HMac(DigestUtilities.GetDigest(hashName));
            _outHmac.Init(new KeyParameter(remoteKey.MacKey));

            if (cipherName == "AES-256" || cipherName == "AES-512")
            {
                _decrypt = new CtrStreamCipher(new AesEngine());
                var p = new ParametersWithIV(new KeyParameter(remoteKey.CipherKey), remoteKey.Iv);
                _decrypt.Init(false, p);

                _encrypt = new CtrStreamCipher(new AesEngine());
                p = new ParametersWithIV(new KeyParameter(localKey.CipherKey), localKey.Iv);
                _encrypt.Init(true, p);
            }
            else
            {
                throw new NotSupportedException($"Cipher '{cipherName}' is not supported.");
            }
        }

        /// <inheritdoc />
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => _stream.CanRead;

        /// <inheritdoc />
        public override bool CanTimeout => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

        /// <inheritdoc />
        public override void SetLength(long value) { throw new NotSupportedException(); }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
#pragma warning disable VSTHRD002 
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            var total = 0;
            while (count > 0)

                // Does the current packet have some unread data?
                if (_inBlock != null && _inBlockOffset < _inBlock.Length)
                {
                    var n = Math.Min(_inBlock.Length - _inBlockOffset, count);
                    Array.Copy(_inBlock, _inBlockOffset, buffer, offset, n);
                    total += n;
                    count -= n;
                    offset += n;
                    _inBlockOffset += n;
                }

                // Otherwise, wait for a new block of data.
                else
                {
                    _inBlock = await ReadPacketAsync(cancellationToken);
                    _inBlockOffset = 0;
                }

            return total;
        }

        /// <summary>
        ///   Read an encrypted and signed packet.
        /// </summary>
        /// <returns>
        ///   The plain text as an array of bytes.
        /// </returns>
        /// <remarks>
        ///   A packet consists of a [uint32 length of packet | encrypted body | hmac signature of encrypted body].
        /// </remarks>
        private async Task<byte[]> ReadPacketAsync(CancellationToken cancel)
        {
            var lengthBuffer = await ReadPacketBytesAsync(4, cancel).ConfigureAwait(false);
            var length =
                (lengthBuffer[0] << 24) |
                (lengthBuffer[1] << 16) |
                (lengthBuffer[2] << 8) |
                lengthBuffer[3];
            if (length <= _outHmac.GetMacSize())
                throw new InvalidDataException($"Invalid secio packet length of {length}.");

            var encryptedData = await ReadPacketBytesAsync(length - _outHmac.GetMacSize(), cancel).ConfigureAwait(false);
            var signature = await ReadPacketBytesAsync(_outHmac.GetMacSize(), cancel).ConfigureAwait(false);

            var hmac = _outHmac;
            var mac = new byte[hmac.GetMacSize()];
            hmac.Reset();
            hmac.BlockUpdate(encryptedData, 0, encryptedData.Length);
            hmac.DoFinal(mac, 0);
            if (!signature.SequenceEqual(mac))
                throw new InvalidDataException("HMac error");

            // Decrypt the data in-place.
            _decrypt.ProcessBytes(encryptedData, 0, encryptedData.Length, encryptedData, 0);
            return encryptedData;
        }

        private async Task<byte[]> ReadPacketBytesAsync(int count, CancellationToken cancel)
        {
            var buffer = new byte[count];
            await _stream.ReadExactAsync(buffer, 0, count, cancel).ConfigureAwait(false);
            return buffer;
        }

        /// <inheritdoc />
        public override void Flush()
        {
#pragma warning disable VSTHRD002 
            FlushAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 
        }

        /// <inheritdoc />
        public override async Task FlushAsync(CancellationToken cancel)
        {
            if (_outStream.Length == 0)
                return;

            var data = _outStream.ToArray(); // plain text
            _encrypt.ProcessBytes(data, 0, data.Length, data, 0);

            var hmac = _inHmac;
            var mac = new byte[hmac.GetMacSize()];
            hmac.Reset();
            hmac.BlockUpdate(data, 0, data.Length);
            hmac.DoFinal(mac, 0);

            var length = data.Length + mac.Length;
            _stream.WriteByte((byte) (length >> 24));
            _stream.WriteByte((byte) (length >> 16));
            _stream.WriteByte((byte) (length >> 8));
            _stream.WriteByte((byte) length);
            await _stream.WriteAsync(data, 0, data.Length, cancel);
            await _stream.WriteAsync(mac, 0, mac.Length, cancel);
            await _stream.FlushAsync(cancel).ConfigureAwait(false);

            _outStream.SetLength(0);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) { _outStream.Write(buffer, offset, count); }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _outStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc />
        public override void WriteByte(byte value) { _outStream.WriteByte(value); }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing) _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
