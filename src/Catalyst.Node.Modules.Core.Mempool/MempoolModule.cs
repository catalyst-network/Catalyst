using System;
using Autofac;
using Catalyst.Helpers.KeyValueStore;
using Catalyst.Helpers.Util;

namespace Catalyst.Node.Modules.Core.Mempool
{
    public class MempoolModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mempoolSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ContainerBuilder Load(ContainerBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));
            builder.Register(c => Mempool.GetInstance(c.Resolve<IKeyValueStore>()))
                .As<IMempoolModule>()
                .InstancePerLifetimeScope();
            return builder;
        }
    }
}