using System;
using Autofac;
using Catalyst.Helpers.KeyValueStore;
using Catalyst.Helpers.Util;
using Dawn;

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
            Guard.Argument(builder, nameof(builder)).NotNull();
            builder.Register(c => Mempool.GetInstance(c.Resolve<IKeyValueStore>()))
                .As<IMempool>()
                .SingleInstance();
            return builder;
        }
    }
}