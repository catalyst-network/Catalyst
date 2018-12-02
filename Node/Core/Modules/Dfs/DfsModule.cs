using ADL.Ipfs;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Dfs
{
    public class DfsModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IDfsSettings dfsSettings)
        {
            builder.Register(c => new DfsService(c.Resolve<IIpfs>(),dfsSettings))
                .As<IDfsService>()
                .InstancePerLifetimeScope();
        }
    }
}