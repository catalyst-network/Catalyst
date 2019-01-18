using Autofac;

namespace Catalyst.Node.Modules.Core.Contract
{
    public class ContractModule : ModuleBase, IContractModule
    {
        private readonly IContract Contract;
        private IContractSettings ContractSettings;

        /// <summary>
        /// </summary>
        public ContractModule(IContract contract, IContractSettings contractSettings)
        {
            Contract = contract;
            ContractSettings = contractSettings;
        }

        /// <summary>
        ///     Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IContract GetImpl()
        {
            return Contract;
        }

        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="contractSettings"></param>
        public static ContainerBuilder Load(ContainerBuilder builder, IContractSettings contractSettings)
        {
            builder.Register(c => new ContractModule(c.Resolve<IContract>(), contractSettings))
                .As<IContractModule>()
                .InstancePerLifetimeScope();
            return builder;
        }
    }
}