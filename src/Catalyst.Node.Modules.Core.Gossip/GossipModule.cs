using Autofac;
using Dawn;

namespace Catalyst.Node.Modules.Core.Gossip
{
    public class GossipModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="dfsSettings"></param>
        protected override void Load (ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.Register(c => Gossip.GetInstance())
                .As<IGossip>()
                .SingleInstance();
        }
    }
}