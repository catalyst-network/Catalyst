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
using System.Threading;
using System.Threading.Tasks;
using MultiFormats;

namespace Lib.P2P.Multiplex
{
    /// <summary>
    ///   The header of a multiplex message.
    /// </summary>
    /// <remarks>
    ///   The header of a multiplex message contains the <see cref="StreamId"/> and
    ///   <see cref="PacketType"/> encoded as a <see cref="Varint">variable integer</see>.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/mplex"/>
    public struct Header
    {
        /// <summary>
        ///   The largest possible value of a <see cref="StreamId"/>.
        /// </summary>
        /// <value>
        ///   long.MaxValue >> 3.
        /// </value>
        public const long MaxStreamId = long.MaxValue >> 3;

        /// <summary>
        ///   The smallest possible value of a <see cref="StreamId"/>.
        /// </summary>
        /// <value>
        ///   Zero.
        /// </value>
        public const long MinStreamId = 0;

        /// <summary>
        ///   The stream identifier.
        /// </summary>
        /// <value>
        ///   The session initiator allocates odd IDs and the session receiver allocates even IDs.
        /// </value>
        public long StreamId;

        /// <summary>
        ///   The purpose of the multiplex message.
        /// </summary>
        /// <value>
        ///   One of the <see cref="PacketType"/> enumeration values.
        /// </value>
        public PacketType PacketType;

        /// <summary>
        ///   Writes the header to the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The destination <see cref="Stream"/> for the header.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public async Task WriteAsync(Stream stream, CancellationToken cancel = default(CancellationToken))
        {
            var header = (StreamId << 3) | (long) PacketType;
            await Varint.WriteVarintAsync(stream, header, cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Reads the header from the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The source <see cref="Stream"/> for the header.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.  The task's result
        ///   is the decoded <see cref="Header"/>.
        /// </returns>
        public static async Task<Header> ReadAsync(Stream stream, CancellationToken cancel = default(CancellationToken))
        {
            var varint = await Varint.ReadVarint64Async(stream, cancel).ConfigureAwait(false);
            return new Header
            {
                StreamId = varint >> 3,
                PacketType = (PacketType) ((byte) varint & 0x7)
            };
        }
    }
}
