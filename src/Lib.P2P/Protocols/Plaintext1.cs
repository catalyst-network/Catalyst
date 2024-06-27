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
using Semver;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   TODO
    /// </summary>
    public class Plaintext1 : IEncryptionProtocol
    {
        /// <inheritdoc />
        public string Name { get; } = "plaintext";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1);

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection,
            Stream stream,
            CancellationToken cancel = default)
        {
            connection.SecurityEstablished.SetResult(true);
            await connection.EstablishProtocolAsync("/multistream/", CancellationToken.None).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<Stream> EncryptAsync(PeerConnection connection,
            CancellationToken cancel = default)
        {
            connection.SecurityEstablished.SetResult(true);
            return Task.FromResult(connection.Stream);
        }
    }
}
