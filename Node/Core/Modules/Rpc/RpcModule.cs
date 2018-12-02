using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Rpc
{
    public class RpcModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IRpcSettings rpcSettings)
        {
            builder.Register(c => new RpcService(c.Resolve<IRpcServer>(), rpcSettings))
                .As<IRpcService>()
                .InstancePerLifetimeScope();
        }
    }
}