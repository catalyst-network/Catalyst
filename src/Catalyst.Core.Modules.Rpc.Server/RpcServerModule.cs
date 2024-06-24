using System;
using Autofac;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Modules.Rpc.Server.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server
{
    public sealed class RpcServerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RpcServerChannelFactory>().As<ITcpServerChannelFactory>().SingleInstance();
            builder.RegisterType<RpcServer>().As<IRpcServer>().SingleInstance();
            builder.RegisterType<RpcServerSettings>().As<IRpcServerSettings>();

            // Adjusted to use an Action<ILifetimeScope> delegate
            builder.RegisterBuildCallback(scope =>
            {
                if (scope == null)
                {
                    throw new ArgumentNullException(nameof(scope));
                }

                var logger = scope.Resolve<ILogger>();
                try
                {
                    var rpcServer = scope.Resolve<IRpcServer>();
                    rpcServer.StartAsync().GetAwaiter().GetResult(); // Synchronously start here
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
