using System;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Mempool
{
    public class MempoolModule : Module
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mempoolSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
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
