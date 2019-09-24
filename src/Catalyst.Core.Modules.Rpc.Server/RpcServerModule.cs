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
using Autofac;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Modules.Rpc.Server.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server
{
    public class RpcServerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RpcServerChannelFactory>().As<ITcpServerChannelFactory>().SingleInstance();
            builder.RegisterType<RpcServer>().As<IRpcServer>().SingleInstance();
            builder.RegisterType<RpcServerSettings>().As<IRpcServerSettings>();

            builder.RegisterBuildCallback(async container =>
            {
                var logger = container.Resolve<ILogger>();
                try
                {
                    var rpcServer = container.Resolve<IRpcServer>();
                    await rpcServer.StartAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error loading API");
                }
            });
            
            base.Load(builder);
        }  
    }
}
