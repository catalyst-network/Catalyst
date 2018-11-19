using ADL.Mempool;
using Akka.Actor;

namespace ADL.Node.Ledger
{
    public interface IADL : ILedger
    {
        IActorRef ConsensusService { get; set; }

        IMempool MempoolService { get; set; }
        
        IActorRef GossipService { get; set; }
    }
}