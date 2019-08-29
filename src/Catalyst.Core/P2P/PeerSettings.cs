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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Catalyst.Abstractions.P2P;
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Core.P2P
{
    /// <summary>
    ///     Peer settings class.
    /// </summary>
    public sealed class PeerSettings
        : IPeerSettings
    {
        private readonly Protocol.Common.Network _network;
        public Protocol.Common.Network Network => _network;
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

        public PeerSettings(Protocol.Common.Network network,
            string publicKey,
            int port,
            string payoutAddress,
            IPAddress bindAddress,
            IPAddress publicAddress,
            IList<string> seedServers)
        {
            _network = network;
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
            Enum.TryParse(section.GetSection("Network").Value, out _network);            
            _publicKey = section.GetSection("PublicKey").Value;
            _port = int.Parse((string) section.GetSection("Port").Value);
            _payoutAddress = section.GetSection("PayoutAddress").Value;
            _bindAddress = IPAddress.Parse((string) section.GetSection("BindAddress").Value);
            PublicIpAddress = IPAddress.Parse((string) section.GetSection("PublicIpAddress").Value);
            _seedServers = Enumerable.ToList<string>(section.GetSection("SeedServers").GetChildren().Select(p => p.Value));
        }
    }
}
