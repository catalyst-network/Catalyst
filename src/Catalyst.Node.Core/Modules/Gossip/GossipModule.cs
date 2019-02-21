using Autofac;
using Catalyst.Node.Common.Modules;
using Dawn;

namespace Catalyst.Node.Core.Modules.Gossip
{
    public class GossipModule : Module
    {
        private readonly int _nameProvider;

        public GossipModule(int nameProvider)
        {
            _nameProvider = nameProvider;
        }
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="dfsSettings"></param>
        protected override void Load(ContainerBuilder builder)
        {
            Guard.Argument(builder, nameof(builder)).NotNull();
            if (_nameProvider == 2)
                builder.RegisterType<NameProvider2>().As<INameProvider>().InstancePerDependency();
            else
                builder.RegisterType<NameProvider1>().As<INameProvider>();

            builder.RegisterType<Gossip>().As<IGossip>().SingleInstance();
        }
    }
}