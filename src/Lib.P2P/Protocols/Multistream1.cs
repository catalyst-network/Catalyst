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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Semver;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   A protocol to select other protocols.
    /// </summary>
    /// <seealso href="https://github.com/multiformats/multistream-select"/>
    public sealed class Multistream1 : IPeerProtocol
    {
        private static ILog _log = LogManager.GetLogger(typeof(Multistream1));

        /// <inheritdoc />
        public string Name { get; } = "multistream";

        /// <inheritdoc />
        public SemVersion Version { get; } = new(1);

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection,
            Stream stream,
            CancellationToken cancel = default)
        {
            var msg = await Message.ReadStringAsync(stream, cancel).ConfigureAwait(false);

            // TODO: msg == "ls"
            if (msg == "ls") throw new NotImplementedException("multistream ls");

            // Switch to the specified protocol
            if (!connection.Protocols.TryGetValue(msg, out var protocol))
            {
                await Message.WriteAsync("na", stream, cancel).ConfigureAwait(false);
                return;
            }

            // Ack protocol switch
            _log.Debug("switching to " + msg);
            await Message.WriteAsync(msg, stream, cancel).ConfigureAwait(false);

            // Process protocol message.
            await protocol(connection, stream, cancel).ConfigureAwait(false);
        }
    }
}
