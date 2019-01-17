using System;
using System.Collections.Generic;
using Autofac;
using Catalyst.Helpers.Util;
using Google.Protobuf;

namespace Catalyst.Node.Modules.Core.Mempool
{
    public class MempoolModule : ModuleBase, IMempoolModule
    {
        private readonly IMempool Mempool;
        private IMempoolSettings MempoolSettings;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mempoolSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ContainerBuilder Load(ContainerBuilder builder, IMempoolSettings mempoolSettings)
        {
            Guard.NotNull(builder,nameof(builder));
            Guard.NotNull(mempoolSettings,nameof(mempoolSettings));
            builder.Register(c => new MempoolModule(c.Resolve<IMempool>(), mempoolSettings))
                .As<IMempoolModule>()
                .InstancePerLifetimeScope();
            return builder;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public MempoolModule(IMempool mempool, IMempoolSettings settings)
        {
            Guard.NotNull(mempool,nameof(mempool));
            Guard.NotNull(settings,nameof(settings));
            Mempool = mempool;
            MempoolSettings = settings;
        }

        /// <summary>
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IMempool GetImpl()
        {
            return Mempool;
        }
    }
}