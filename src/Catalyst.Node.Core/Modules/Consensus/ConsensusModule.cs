using Autofac;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cryptography;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Node.Core.Modules.Consensus
{
    public class ConsensusModule : JsonConfiguredModule
    {
        public ConsensusModule(string configFilePath) : base(configFilePath) { }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BLAKE2B_256>().As<IMultihashAlgorithm>();
            builder.RegisterType<IsaacRandomFactory>().As<IDeterministicRandomFactory>();
            base.Load(builder);
        }
    }
}
