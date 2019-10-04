using Autofac;
using Catalyst.Abstractions.Hashing;
using Ipfs.Registry;

namespace Catalyst.Core.Modules.Hashing
{
    public class HashingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var hashingAlgorithm = HashingAlgorithm.GetAlgorithmMetadata("blake2b-256");
            builder.RegisterInstance(hashingAlgorithm).SingleInstance();
            builder.RegisterType<HashingProvider>().As<IHashProvider>().SingleInstance();
        }
    }
}
