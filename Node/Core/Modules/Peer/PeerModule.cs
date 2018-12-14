using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Peer
{
    public class PeerModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IPeerSettings peerSettings, ISslSettings sslSettings, NodeOptions options)
        {
            builder.Register(c => new PeerService(peerSettings, sslSettings, options))
                .As<IPeerService>()
                .InstancePerLifetimeScope();
        }
    }
}