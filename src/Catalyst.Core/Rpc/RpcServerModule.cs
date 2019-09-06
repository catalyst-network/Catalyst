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
using System.Reactive.Concurrency;
using Autofac;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Rpc.Authentication;
using Catalyst.Core.Rpc.Authentication.Models;
using Catalyst.Core.Rpc.Authentication.Repository;
using Catalyst.Core.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Rpc.IO.Transport.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;

namespace Catalyst.Core.Rpc
{
    public class RpcServerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new InMemoryRepository<AuthCredentials>(
                c.ResolveOptional<ICachingStrategy<AuthCredentials, int>>()
                ))
                .As<IRepository<AuthCredentials, string>>();

            // should go in its own separate auth module
            builder.Register(c => new AuthCredentialRepository(
                    c.Resolve<IRepository<AuthCredentials, string>>()
                    ))
                .As<IAuthCredentialRepository>();
            
            builder.Register(c => new RpcServerChannelFactory(
                    c.Resolve<IRpcMessageCorrelationManager>(),
                    c.Resolve<IKeySigner>(),
                    c.Resolve<IAuthenticationStrategy>(),
                    c.Resolve<IPeerIdValidator>(),
                    c.Resolve<ISigningContextProvider>(),
                    c.ResolveOptional<IScheduler>()
                ))
                .As<ITcpServerChannelFactory>();
            
            builder.Register(c => new RepositoryAuthenticationStrategy(
                    c.Resolve<IAuthCredentialRepository>()
                ))
                .As<IAuthenticationStrategy>();
                
            builder.Register(c => new RpcMessageCorrelationManager(
                    c.Resolve<IMemoryCache>(),
                    c.Resolve<ILogger>(),
                    c.Resolve<IChangeTokenProvider>(),
                    c.ResolveOptional<IScheduler>()
                    ))
                .As<IRpcMessageCorrelationManager>()
                .SingleInstance();

            builder.Register(c => new RpcServerSettings(
                    c.Resolve<IConfigurationRoot>()
                    ))
                .As<IRpcServerSettings>();

            builder.Register(c => new RpcServer(c.Resolve<IRpcServerSettings>(),
                    c.Resolve<ILogger>(),
                    c.Resolve<ITcpServerChannelFactory>(),
                    c.Resolve<ICertificateStore>(),
                    c.Resolve<IEnumerable<IRpcRequestObserver>>(),
                    c.Resolve<ITcpServerEventLoopGroupFactory>()
                ))
               .As<IRpcServer>().SingleInstance();
        }  
    }
}
