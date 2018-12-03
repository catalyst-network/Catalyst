using ADL.Ipfs;
using ADL.Node.Core.Modules.Consensus;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Consensus
{
    public class ConsensusModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IConsensusSettings consensusSettings)
        {
            builder.Register(c => new ConsensusService(c.Resolve<IConsensus>(), consensusSettings))
                .As<IConsensusService>()
                .InstancePerLifetimeScope();
        }
    }
}