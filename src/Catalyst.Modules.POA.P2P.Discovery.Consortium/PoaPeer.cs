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
using System.Net;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Peer;

namespace Catalyst.Modules.POA.P2P
{
    [Obsolete("Please use the PeerIdDao instead")]
    public sealed class PoaPeer
    {
        public string Ip { get; set; }

        public int Port { get; set; }

        public string PublicKey { get; set; }

        public PeerId ToPeerId()
        {
            return new PeerId
            {
                PublicKey = PublicKey.KeyToByteString(),
                Ip = IPAddress.Parse(Ip).To16Bytes().ToByteString(),
                Port = (uint) Port
            };
        }
    }
}
