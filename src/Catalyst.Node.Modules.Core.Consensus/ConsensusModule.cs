using Autofac;

namespace Catalyst.Node.Modules.Core.Consensus
{
    public class ConsensusModule : ModuleBase, IConsensusService
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

        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="consensusSettings"></param>
        public static ContainerBuilder Load(ContainerBuilder builder, IConsensusSettings consensusSettings)
        {
            builder.Register(c => new ConsensusModule(c.Resolve<IConsensus>(), consensusSettings))
                .As<IConsensusService>()
                .InstancePerLifetimeScope();
            return builder;
        }
    }
}