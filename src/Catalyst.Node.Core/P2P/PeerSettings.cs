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
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.P2P
{
    /// <summary>
    ///     Peer settings class.
    /// </summary>
    public sealed class PeerSettings
        : IPeerSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="rootSection"></param>
        public PeerSettings(IConfigurationRoot rootSection)
        {
            Guard.Argument(rootSection, nameof(rootSection)).NotNull();
            var section = rootSection.GetSection("CatalystNodeConfiguration").GetSection("Peer");
            Network = Enumeration.Parse<Network>(section.GetSection("Network").Value);
            PublicKey = section.GetSection("PublicKey").Value;
            Port = int.Parse(section.GetSection("Port").Value);
            Magic = int.Parse(section.GetSection("Magic").Value);
            PfxFileName = section.GetSection("PfxFileName").Value;
            PayoutAddress = section.GetSection("PayoutAddress").Value;
            Announce = bool.Parse(section.GetSection("Announce").Value);
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
            AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
            KnownNodes = section.GetSection("KnownNodes").GetChildren().Select(p => p.Value).ToList();
            SeedServers = section.GetSection("SeedServers").GetChildren().Select(p => p.Value).ToList();
            AnnounceServer =
                Announce ? EndpointBuilder.BuildNewEndPoint(section.GetSection("AnnounceServer").Value) : null;
        }

        public Network Network { get; }
        public string PayoutAddress { get; }
        public string PublicKey { get; }
        public bool Announce { get; }
        public IPEndPoint AnnounceServer { get; }
        public int Port { get; }
        public IPEndPoint EndPoint => new IPEndPoint(BindAddress, Port);
        public int Magic { get; }
        public IPAddress BindAddress { get; }
        public string PfxFileName { get; }
        public IList<string> KnownNodes { get; }
        public IList<string> SeedServers { get; }
        public byte AddressVersion { get; }
    }
}
