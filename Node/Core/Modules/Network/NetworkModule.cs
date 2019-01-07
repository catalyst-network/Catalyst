using System;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Network
{
    public class NetworkModule : Module
    {
        public void Load(
            ContainerBuilder builder,
            INetworkSettings networkSettings,
            ISslSettings sslSettings,
            NodeOptions options
        )
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (networkSettings == null) throw new ArgumentNullException(nameof(networkSettings));

            builder.Register(c => new NetworkService(networkSettings, sslSettings, options))
                .As<INetworkService>()
                .InstancePerLifetimeScope();
        }
    }
}
