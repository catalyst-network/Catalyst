using System;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Peer
{
    public class PeerModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IPeerSettings peerSettings, ISslSettings sslSettings, NodeOptions options)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (peerSettings == null) throw new ArgumentNullException(nameof(peerSettings));
            if (sslSettings == null) throw new ArgumentNullException(nameof(sslSettings));
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            builder.Register(c => new PeerService(peerSettings, sslSettings, options))
                .As<IPeerService>()
                .InstancePerLifetimeScope();
        }
    }
}