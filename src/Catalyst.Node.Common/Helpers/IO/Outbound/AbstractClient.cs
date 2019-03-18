/*
* Copyright(c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
*/

using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Outbound
{
    public abstract class AbstractClient<T, W> : AbstractIo<T, W> where T : ISocketClient where W : IBootstrap
    {
        protected IBootstrap Client { private get; set; }
        
        protected internal AbstractClient(ILogger logger) : base(logger) {}

        public async Task<AbstractClient<T, W>> ConnectClient(IPAddress listenAddress, int port)
        {
            Channel = await Client.ConnectAsync(listenAddress, port).ConfigureAwait(false);
            return this;
        }        
    }
}
