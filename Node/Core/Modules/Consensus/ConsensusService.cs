using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Consensus
{
    public class ConsensusService : ServiceBase, IConsensusService
    {
        private IConsensus Consensus;
        private IConsensusSettings ConsensusSettings;
        
        /// <summary>
        /// 
        /// </summary>
        public ConsensusService(IConsensus consensus, IConsensusSettings consensusSettings)
        {
            Consensus = consensus;
            ConsensusSettings = consensusSettings;
        }

        /// <summary>
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IConsensus GetImpl()
        {
            return Consensus;
        }
    }
}
