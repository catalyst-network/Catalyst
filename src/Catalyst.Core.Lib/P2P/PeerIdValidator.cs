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
using System.Net;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Network;
using Catalyst.Protocol.Peer;
using Dawn;

namespace Catalyst.Core.Lib.P2P
{
    /// <summary>
    ///     @TODO move to SDK
    /// </summary>
    public sealed class PeerIdValidator : IPeerIdValidator
    {
        private const int PeerIdKeyLength = 48;
        private readonly ICryptoContext _cryptoContext;

        public PeerIdValidator(ICryptoContext cryptoContext)
        {
            _cryptoContext = cryptoContext;
        }

        /// <inheritdoc cref="Catalyst.Abstractions.P2P.IPeerIdValidator"/>
        public bool ValidatePeerIdFormat(PeerId peerId)
        {
            Guard.Argument(peerId, nameof(peerId)).NotNull()
               .Require(p => p.Ip.Length == 16 && ValidateIp(p.Ip.ToByteArray()), _ => "Ip should be 16 bytes")
               .Require(p => ValidatePort(p.Port), _ => "Port should be between 1025 and 65535")
               .Require(p => p.PublicKey.Length == PeerIdKeyLength, p => $"PublicKey should be {PeerIdKeyLength} bytes but was {p.PublicKey.Length}");

            return true;
        }

        /// <summary>Validates the raw PID chunks.</summary>
        /// <param name="peerIdChunks">The peer identifier chunks.</param>
        /// <returns></returns>
        public void ValidateRawPidChunks(IReadOnlyList<string> peerIdChunks)
        {
            Guard.Argument(peerIdChunks).Count(5);
            Guard.Argument(peerIdChunks[0]).Length(2);
            Guard.Argument(peerIdChunks[1]).Length(2);
            Guard.Argument(peerIdChunks[2]).Length(14);
            Guard.Argument(peerIdChunks[3]).MinLength(4).MaxLength(5);
            Guard.Argument(peerIdChunks[4]).Length(_cryptoContext.PublicKeyLength);
        }

        /// <summary>
        /// </summary>
        /// <param name="clientIp"></param>
        private bool ValidateIp(byte[] clientIp)
        {
            return IPAddress.TryParse(new IPAddress(clientIp).ToString(), out _);
        }

        /// <summary>
        /// </summary>
        /// <param name="portBytes"></param>
        private bool ValidatePort(uint portBytes)
        {
            return Ip.ValidPortRange((ushort) portBytes);
        }
    }
}
