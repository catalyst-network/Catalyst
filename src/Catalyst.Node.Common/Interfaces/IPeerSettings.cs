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
using Catalyst.Node.Common.Helpers.Config;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IPeerSettings
    {
        Network Network { get; }
        string PayoutAddress { get; }
        string PublicKey { get; }
        bool Announce { get; }
        IPEndPoint AnnounceServer { get; }
        bool MutualAuthentication { get; }
        bool AcceptInvalidCerts { get; }
        ushort MaxConnections { get; }
        int Port { get; }
        IPAddress BindAddress { get; }
        IPEndPoint EndPoint { get; }
        int Magic { get; }
        string PfxFileName { get; }
        List<string> KnownNodes { get; }
        List<string> SeedServers { get; }
        byte AddressVersion { get; }
        string SslCertPassword { get; }
    }
}