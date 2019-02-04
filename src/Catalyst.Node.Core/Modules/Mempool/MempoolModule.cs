using System;
using Autofac;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;
using Dawn;

namespace Catalyst.Node.Core.Modules.Mempool
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
            builder.Register(c => new Mempool(c.Resolve<IKeyValueStore>()))
                .As<IMempool>()
                .SingleInstance();
            return builder;
        }
    }
}