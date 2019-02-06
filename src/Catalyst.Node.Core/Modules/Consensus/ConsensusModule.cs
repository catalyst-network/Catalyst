using Autofac;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Consensus
{
    public class ConsensusModule : Module
    {
        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="consensusSettings"></param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new Consensus())
                   .As<IConsensus>()
                   .SingleInstance();
        }
    }
}