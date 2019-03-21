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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Dawn;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Common.Helpers.IO
{
    public abstract class AbstractChannelInitializer<T> :  ChannelInitializer<T> where T : IChannel
    {
        protected readonly Action<T> InitializationAction;
        protected readonly X509Certificate Certificate;
        protected readonly IReadOnlyCollection<IChannelHandler> Handlers;
        protected readonly IPAddress TargetHost;

        protected AbstractChannelInitializer(
            Action<T> initializationAction,
            IList<IChannelHandler> handlers,
            IPAddress targetHost = default,
            X509Certificate certificate = null
        )
        {
            
            Guard.Argument(handlers, nameof(handlers)).NotNull().NotEmpty();
            InitializationAction = initializationAction;
            Handlers = handlers.ToImmutableArray();
            TargetHost = targetHost;
            Certificate = certificate;
        }
    }
}