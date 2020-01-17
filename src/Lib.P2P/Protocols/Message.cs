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

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using MultiFormats;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   A message that is exchanged between peers.
    /// </summary>
    /// <remarks>
    ///   A message consists of
    ///   <list type="bullet">
    ///      <item><description>A <see cref="Varint"/> length prefix</description></item>
    ///      <item><description>The payload</description></item>
    ///      <item><description>A terminating newline</description></item>
    ///   </list>
    /// </remarks>
    public static class Message
    {
        private static byte[] _newline = new byte[] {0x0a};
        private static ILog _log = LogManager.GetLogger(typeof(Message));

        /// <summary>
        ///   Read the message as a sequence of bytes from the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="Stream"/> to a peer.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the byte representation of the message's payload.
        /// </returns>
        /// <exception cref="InvalidDataException">
        ///   When the message is invalid.
        /// </exception>
        public static async Task<byte[]> ReadBytesAsync(Stream stream,
            CancellationToken cancel = default)
        {
            var eol = new byte[1];
            var length = await stream.ReadVarint32Async(cancel).ConfigureAwait(false);
            var buffer = new byte[length - 1];
            await stream.ReadExactAsync(buffer, 0, length - 1, cancel).ConfigureAwait(false);
            await stream.ReadExactAsync(eol, 0, 1, cancel).ConfigureAwait(false);
            if (eol[0] != _newline[0])
            {
                _log.Error($"length: {length}, bytes: {buffer.ToHexString()}");
                throw new InvalidDataException("Missing terminating newline");
            }

            return buffer;
        }

        /// <summary>
        ///   Read the message as a <see cref="string"/> from the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="Stream"/> to a peer.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the string representation of the message's payload.
        /// </returns>
        /// <exception cref="InvalidDataException">
        ///   When the message is invalid.
        /// </exception>
        /// <remarks>
        ///   The return value has the length prefix and terminating newline removed.
        /// </remarks>
        public static async Task<string> ReadStringAsync(Stream stream,
            CancellationToken cancel = default)
        {
            var payload = Encoding.UTF8.GetString(await ReadBytesAsync(stream, cancel).ConfigureAwait(false));

            _log.Trace("received " + payload);
            return payload;
        }

        /// <summary>
        ///   Writes the binary representation of the message to the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="message">
        ///   The message to write.  A newline is automatically appended.
        /// </param>
        /// <param name="stream">
        ///   The <see cref="Stream"/> to a peer.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public static async Task WriteAsync(string message,
            Stream stream,
            CancellationToken cancel = default)
        {
            _log.Trace("sending " + message);

            var payload = Encoding.UTF8.GetBytes(message);
            await stream.WriteVarintAsync(message.Length + 1, cancel).ConfigureAwait(false);
            await stream.WriteAsync(payload, 0, payload.Length, cancel).ConfigureAwait(false);
            await stream.WriteAsync(_newline, 0, _newline.Length, cancel).ConfigureAwait(false);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }
    }
}
