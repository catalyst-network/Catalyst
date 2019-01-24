using Autofac;

namespace Catalyst.Node.Modules.Core.Consensus
{
    public class ConsensusModule : ModuleBase, IConsensusModule
    {
        private readonly IConsensus Consensus;
        private IConsensusSettings ConsensusSettings;

        /// <summary>
        /// </summary>
        public ConsensusModule(IConsensus consensus, IConsensusSettings consensusSettings)
        {
            Consensus = consensus;
            ConsensusSettings = consensusSettings;
        }

        /// <summary>
        ///     Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IConsensus GetImpl()
        {
            return Consensus;
        }
    }
}