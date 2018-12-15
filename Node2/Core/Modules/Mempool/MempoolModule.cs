using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Mempool
{
    public class MempoolModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IMempoolSettings mempoolSettings)
        {
            builder.Register(c => new MempoolService(c.Resolve<IMempool>(), mempoolSettings))
                .As<IMempoolService>()
                .InstancePerLifetimeScope();
        }
    }
}