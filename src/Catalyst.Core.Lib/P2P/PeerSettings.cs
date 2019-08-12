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
using System.Linq;
using System.Net;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Types;
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Core.Lib.P2P
{
    /// <summary>
    ///     Peer settings class.
    /// </summary>
    public sealed class PeerSettings
        : IPeerSettings
    {
        private readonly NetworkTypes _networkTypes;
        public NetworkTypes NetworkTypes => _networkTypes;
        private readonly string _publicKey;
        public string PublicKey => _publicKey;
        private readonly int _port;
        public int Port => _port;
        private readonly string _payoutAddress;
        public string PayoutAddress => _payoutAddress;
        private readonly IPAddress _bindAddress;
        public IPAddress BindAddress => _bindAddress;
        private readonly IList<string> _seedServers;
        public IList<string> SeedServers => _seedServers;
        
        public IPAddress PublicIpAddress { get; }

        public PeerSettings(NetworkTypes networkTypes,
            string publicKey,
            int port,
            string payoutAddress,
            IPAddress bindAddress,
            IPAddress publicAddress,
            IList<string> seedServers)
        {
            _networkTypes = networkTypes;
            _publicKey = publicKey;
            _port = port;
            _payoutAddress = payoutAddress;
            _bindAddress = bindAddress;
            PublicIpAddress = publicAddress;
            _seedServers = seedServers;
        }
            
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="rootSection"></param>
        public PeerSettings(IConfigurationRoot rootSection)
        {
            Guard.Argument(rootSection, nameof(rootSection)).NotNull();
            var section = rootSection.GetSection("CatalystNodeConfiguration").GetSection("Peer");
            _networkTypes = Enumeration.Parse<NetworkTypes>(section.GetSection("Network").Value);
            _publicKey = section.GetSection("PublicKey").Value;
            _port = int.Parse(section.GetSection("Port").Value);
            _payoutAddress = section.GetSection("PayoutAddress").Value;
            _bindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
            PublicIpAddress = IPAddress.Parse(section.GetSection("PublicIpAddress").Value);
            _seedServers = section.GetSection("SeedServers").GetChildren().Select(p => p.Value).ToList();
        }
    }
}
