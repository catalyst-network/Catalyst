using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Gossip
{
    public class GossipModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IGossipSettings gossipSettings)
        {
            builder.Register(c => new GossipService(gossipSettings))
                .As<IGossipService>()
                .InstancePerLifetimeScope();
        }
    }
}