using System;
using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Mempool
{
    public class MempoolService : ServiceBase, IMempoolService
    {
        private readonly IMempool Mempool;
        private IMempoolSettings MempoolSettings;
        
        /// <summary>
        /// 
        /// </summary>
        public MempoolService(IMempool mempool, IMempoolSettings settings)
        {
            if (mempool == null) throw new ArgumentNullException(nameof(mempool));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
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