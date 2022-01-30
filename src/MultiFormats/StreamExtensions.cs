#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Threading;
using System.Threading.Tasks;

namespace MultiFormats
{
    /// <summary>
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        ///     Asynchronously reads a sequence of bytes from the stream and advances
        ///     the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="stream">
        ///     The stream to read from.
        /// </param>
        /// <param name="buffer">
        ///     The buffer to write the data into.
        /// </param>
        /// <param name="offset">
        ///     The byte offset in <paramref name="buffer" /> at which to begin
        ///     writing data from the <paramref name="stream" />.
        /// </param>
        /// <param name="length">
        ///     The number of bytes to read.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="EndOfStreamException">
        ///     When the <paramref name="stream" /> does not have
        ///     <paramref name="length" /> bytes.
        /// </exception>
        public static async Task ReadExactAsync(this Stream stream, byte[] buffer, int offset, int length)
        {
            while (0 < length)
            {
                var n = await stream.ReadAsync(buffer, offset, length);
                if (n == 0) throw new EndOfStreamException();

                offset += n;
                length -= n;
            }
        }

        /// <summary>
        ///     Asynchronously reads a sequence of bytes from the stream and advances
        ///     the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="stream">
        ///     The stream to read from.
        /// </param>
        /// <param name="buffer">
        ///     The buffer to write the data into.
        /// </param>
        /// <param name="offset">
        ///     The byte offset in <paramref name="buffer" /> at which to begin
        ///     writing data from the <paramref name="stream" />.
        /// </param>
        /// <param name="length">
        ///     The number of bytes to read.
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="EndOfStreamException">
        ///     When the <paramref name="stream" /> does not have
        ///     <paramref name="length" /> bytes.
        /// </exception>
        public static async Task ReadExactAsync(this Stream stream,
            byte[] buffer,
            int offset,
            int length,
            CancellationToken cancel)
        {
            while (0 < length)
            {
                var n = await stream.ReadAsync(buffer, offset, length, cancel)
                   .ConfigureAwait(false);

                if (n == 0) throw new EndOfStreamException();

                offset += n;
                length -= n;
            }
        }
    }
}
