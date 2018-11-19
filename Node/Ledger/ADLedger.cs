using ADL.Mempool;
using Akka.Actor;

namespace ADL.Node.Ledger
{
    /// <summary>
    /// Ledger - Ledger Class
    /// </summary>
    internal class ADLedger : IADL
    {        
        public IActorRef ConsensusService { get; set; }

        public IMempool MempoolService { get; set; }
        
        public IActorRef GossipService { get; set; }
        
        /// <summary>
        /// Ledger constructor.
        /// </summary>
        internal ADLedger(IActorRef consensusService, IMempool mempoolService, IActorRef gossipService)
        {
            ConsensusService = consensusService;
            MempoolService = mempoolService;
            GossipService = gossipService;
        }
    }
}
