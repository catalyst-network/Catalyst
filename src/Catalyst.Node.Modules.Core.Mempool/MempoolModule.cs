using System;
using Autofac;
using Catalyst.Helpers.Util;

namespace Catalyst.Node.Modules.Core.Mempool
{
    public class MempoolModule : ModuleBase, IMempoolModule
    {
        private readonly IMempool Mempool;
        private readonly IMempoolSettings MempoolSettings;

        /// <summary>
        /// </summary>
        public MempoolModule(IMempool mempool, IMempoolSettings settings)
        {
            Guard.NotNull(mempool, nameof(mempool));
            Guard.NotNull(settings, nameof(settings));
            Mempool = mempool;
            MempoolSettings = settings;
            Mempool._keyValueStore.Connect(MempoolSettings.Host);
        }

        /// <summary>
        ///     Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IMempool GetImpl()
        {
            return Mempool;
        }
    }
}