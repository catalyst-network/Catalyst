using Autofac;
using Catalyst.Abstractions.Hashing;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Core.Modules.Hashing
{
    public class HashingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BLAKE2B_256>().As<IMultihashAlgorithm>();
            builder.RegisterType<Blake2bHashingProvider>().As<IHashProvider>();
        }
    }
}
