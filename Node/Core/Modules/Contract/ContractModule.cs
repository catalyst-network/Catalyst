using ADL.Ipfs;
using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Contract
{
    public class ContractModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, IContractSettings contractSettings)
        {
            builder.Register(c => new ContractService(c.Resolve<IContract>(), contractSettings))
                .As<IContractService>()
                .InstancePerLifetimeScope();
        }
    }
}