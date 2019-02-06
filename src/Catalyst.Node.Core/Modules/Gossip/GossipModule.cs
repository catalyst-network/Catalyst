using Autofac;
using Catalyst.Node.Common.Modules;
using Dawn;

namespace Catalyst.Node.Core.Modules.Gossip
{
    public class GossipModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="dfsSettings"></param>
        protected override void Load(ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.Register(c => new Gossip())
                   .As<IGossip>()
                   .SingleInstance();
        }
    }
}