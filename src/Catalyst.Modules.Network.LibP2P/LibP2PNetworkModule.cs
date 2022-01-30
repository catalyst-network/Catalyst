#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Autofac;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.P2P;
using Lib.P2P.Protocols;

namespace Catalyst.Modules.Network.LibP2P
{
    public class LibP2PNetworkModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CatalystProtocol>().AsImplementedInterfaces().SingleInstance();

            // Register P2P
            builder.RegisterType<LibP2PPeerService>().As<IPeerService>().SingleInstance();
            builder.RegisterType<LibP2PPeerClient>().As<IPeerClient>().SingleInstance();
        }
    }
}
