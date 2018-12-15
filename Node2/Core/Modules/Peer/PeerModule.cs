using System;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Peer
{
    public class PeerModule : Module
    {
        public void Load(ContainerBuilder builder, IPeerSettings peerSettings, ISslSettings sslSettings, NodeOptions options)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (peerSettings == null) throw new ArgumentNullException(nameof(peerSettings));

            builder.Register(c => new PeerService(peerSettings, sslSettings, options))
                .As<IPeerService>()
                .InstancePerLifetimeScope();
        }
    }
}
