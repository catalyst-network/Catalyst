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

using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Network;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Dawn;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Catalyst.Common.P2P
{
    public sealed class PeerIdValidator : IPeerIdValidator
    {
        private readonly ICryptoContext _cryptoContext;
        private readonly IPeerIdClientId _peerIdClientId;

        public PeerIdValidator(ICryptoContext cryptoContext, IPeerIdClientId clientId)
        {
            _cryptoContext = cryptoContext;
            _peerIdClientId = clientId;
        }

        /// <inheritdoc cref="IPeerIdValidator"/>
        public bool ValidatePeerIdFormat(PeerId peerId)
        {
            var publicKeyLength = _cryptoContext.PublicKeyLength;
            Guard.Argument(peerId, nameof(peerId)).NotNull()
               .Require(p => p.PublicKey.Length == publicKeyLength, _ => $"PublicKey should be {publicKeyLength} bytes")
               .Require(p => p.Ip.Length == 16 && ValidateIp(p.Ip.ToByteArray()), _ => "Ip should be 16 bytes")
               .Require(p => ValidatePort(p.Port.ToByteArray()), _ => "Port should be between 1025 and 65535")
               .Require(p => ValidateClientId(p.ClientId.ToByteArray()),
                    _ => "ClientId should only be 2 alphabetical letters")
               .Require(p => ValidateClientVersion(p.ProtocolVersion.ToByteArray()),
                    _ => $"ClientVersion doesn't match {_peerIdClientId.AssemblyMajorVersion}");
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
        /// <param name="clientId"></param>
        /// <exception cref="ArgumentException"></exception>
        private bool ValidateClientId(byte[] clientId)
        {
            return Regex.IsMatch(ByteUtil.ByteToString(clientId), @"^[a-zA-Z]{1,2}$");
        }

        /// <summary>
        /// </summary>
        /// <param name="clientVersion"></param>
        /// <exception cref="ArgumentException"></exception>
        private bool ValidateClientVersion(byte[] clientVersion)
        {
            Guard.Argument(clientVersion, nameof(clientVersion))
               .NotNull().NotEmpty().Count(2);
            var intVersion = int.Parse(Encoding.UTF8.GetString(clientVersion));
            return 0 <= intVersion && intVersion <= 99;
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
        private bool ValidatePort(byte[] portBytes)
        {
            return Ip.ValidPortRange(BitConverter.ToUInt16(portBytes));
        }
    }
}
