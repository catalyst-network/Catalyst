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
using Catalyst.Node.Core.P2P;
using Catalyst.Common.IO.Outbound;
using NSubstitute;
using Catalyst.Common.Interfaces.Modules.KeySigner;

namespace Catalyst.Node.Core.IntegrationTests.P2P
{
    public class PeerClientFixture : IDisposable
    {
        public PeerClientFixture()
        {
            UniversalPeerClient = new PeerClient(new UdpClientChannelFactory(Substitute.For<IKeySigner>()));
        }

        public void Dispose()
        { 
            UniversalPeerClient.Dispose();
        }

        public PeerClient UniversalPeerClient { get; private set; }
    }
}
