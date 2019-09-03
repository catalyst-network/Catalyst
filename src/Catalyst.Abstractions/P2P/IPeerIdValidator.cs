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

using System.Collections.Generic;
using Catalyst.Protocol.Common;

namespace Catalyst.Abstractions.P2P
{
    /// <summary>
    /// Validates the PeerId object
    /// </summary>
    public interface IPeerIdValidator
    {
        /// <summary>Validates the peer.</summary>
        /// <param name="peerId">The Peer Id <see cref="PeerId"/></param>
        /// <returns>[true] if valid [false] if invalid</returns>
        bool ValidatePeerIdFormat(PeerId peerId);

        /// <summary>Validates the raw pid chunks.</summary>
        /// <param name="peerIdChunks">The peer identifier chunks.</param>
        void ValidateRawPidChunks(IReadOnlyList<string> peerIdChunks);
    }
}
