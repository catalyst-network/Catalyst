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
using System.Security.Cryptography.X509Certificates;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Transport.Channels
{
    public sealed class ServerChannelInitializerBase<T>
        : ChannelInitializerBase<T> where T : IChannel
    {
        /// <inheritdoc />
        public ServerChannelInitializerBase(IList<IChannelHandler> handlers,
            X509Certificate certificate = null)
            : base(handlers,
                certificate == null ? null : TlsHandler.Server(certificate)) { }

        public override string ToString()
        {
            return "InboundChannelInitializer[" + StringUtil.SimpleClassName(typeof(T)) + "]";
        }
    }
}
