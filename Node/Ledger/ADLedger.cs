using Akka.Actor;
using ADL.Mempool;

namespace ADL.Node.Ledger
{
    /// <summary>
    /// Ledger - Ledger Class
    /// </summary>
    internal class ADLedger : IADL
    {        
        public IActorRef GossipService { get; set; }
        public IMempool MempoolService { get; set; }
        public IActorRef ConsensusService { get; set; }
               
        /// <summary>
        /// Ledger constructor.
        /// </summary>
        internal ADLedger(IActorRef consensusService, IMempool mempoolService, IActorRef gossipService)
        {
            GossipService = gossipService;
            MempoolService = mempoolService;
            ConsensusService = consensusService;
        }
    }
}
