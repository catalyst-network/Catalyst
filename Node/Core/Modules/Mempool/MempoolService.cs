using ADL.DataStore;

namespace ADL.Node.Core.Modules.Mempool
{
    public class MempoolService : IMempoolService
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
        
        public bool StartService()
        {
            throw new System.NotImplementedException();
        }

        public bool StopService()
        {
            throw new System.NotImplementedException();
        }

        public bool RestartService()
        {
            throw new System.NotImplementedException();
        }
    }
}