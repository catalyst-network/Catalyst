using System;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Mempool
{
    public class MempoolModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IMempoolSettings mempoolSettings)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (mempoolSettings == null) throw new ArgumentNullException(nameof(mempoolSettings));
            builder.Register(c => new MempoolService(c.Resolve<IMempool>(), mempoolSettings))
                .As<IMempoolService>()
                .InstancePerLifetimeScope();
        }
    }
}