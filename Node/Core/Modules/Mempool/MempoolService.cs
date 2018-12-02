using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Mempool
{
    public class MempoolService : ServiceBase, IMempoolService
    {
        private IMempool Mempool;
        private IMempoolSettings MempoolSettings;
        
        /// <summary>
        /// 
        /// </summary>
        public MempoolService(IMempool mempool, IMempoolSettings settings)
        {
            Mempool = mempool;
            MempoolSettings = settings;
        }
    }
}