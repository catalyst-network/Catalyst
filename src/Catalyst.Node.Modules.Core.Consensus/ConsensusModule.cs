using Autofac;

namespace Catalyst.Node.Modules.Core.Consensus
{
    public class ConsensusModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="consensusSettings"></param>
        public static ContainerBuilder Load(ContainerBuilder builder)
        {
            builder.Register(c => Consensus.GetInstance())
                .As<IConsensus>()
                .SingleInstance();
            return builder;
        }
    }
}