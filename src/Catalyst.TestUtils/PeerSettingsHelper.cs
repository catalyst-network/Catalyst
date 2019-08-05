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
using System.IO;
using System.Linq;
using System.Net;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.Util;
using Multiformats.Hash.Algorithms;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class PeerSettingsHelper
    {
        public static IPeerSettings TestPeerSettings(string publicKey = "publicKey", int port = 42069)
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.Network.Returns(Network.Dev);
            peerSettings.PublicKey.Returns(
                publicKey?.ComputeUtf8Multihash(new ID()).ToBytes().KeyToString() 
             ?? TestKeyRegistry.TestPublicKey);
            peerSettings.Port.Returns(port);
            peerSettings.PayoutAddress.Returns("my_pay_out_address");
            peerSettings.BindAddress.Returns(IPAddress.Loopback);
            peerSettings.SeedServers.Returns(new List<string>
            {
                "seed1.catalystnetwork.io",
                "seed2.catalystnetwork.io",
                "seed3.catalystnetwork.io",
                "seed4.catalystnetwork.io",
                "seed5.catalystnetwork.io"
            });
            return peerSettings;
        }
    }

    public static class RpcServerSettingsHelper
    {
        public static IRpcServerSettings GetRpcServerSettings(int port = 42051)
        {
            var settings = Substitute.For<IRpcServerSettings>();
            settings.Port.Returns(port);
            settings.BindAddress.Returns(IPAddress.Loopback);
            return settings;
        }
    }
}
