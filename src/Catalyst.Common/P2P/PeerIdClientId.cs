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

using System.Reflection;
using System.Text;
using Catalyst.Common.Interfaces.P2P;

namespace Catalyst.Common.P2P
{
    public class PeerIdClientId : IPeerIdClientId
    {
        public PeerIdClientId(string clientId)
        {
            var assemblyMajorVersion2Digits = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString("D2");
            AssemblyMajorVersion = Encoding.UTF8.GetBytes(assemblyMajorVersion2Digits);
            ClientId = Encoding.UTF8.GetBytes(clientId);
        }

        public byte[] ClientId { get; }

        public byte[] AssemblyMajorVersion { get; }
    }
}
