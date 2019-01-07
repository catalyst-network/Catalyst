using System;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Rpc
{
    public class RpcModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IRpcSettings rpcSettings)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (rpcSettings == null) throw new ArgumentNullException(nameof(rpcSettings));
            builder.Register(c => new RpcService(rpcSettings))
                .As<IRpcService>()
                .InstancePerLifetimeScope();
        }
    }
}