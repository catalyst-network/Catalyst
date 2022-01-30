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

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using MultiFormats;

namespace Catalyst.Modules.Network.Dotnetty.IO.Transport.Channels
{
    public abstract class UdpClientChannelFactory<T> : UdpChannelFactoryBase, IUdpClientChannelFactory<T>
    {
        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress"></param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public abstract Task<IObservableChannel<T>> BuildChannelAsync(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            MultiAddress address,
            X509Certificate2 certificate = null);
    }
}
