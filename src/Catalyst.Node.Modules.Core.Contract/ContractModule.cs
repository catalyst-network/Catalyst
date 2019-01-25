using Autofac;

namespace Catalyst.Node.Modules.Core.Contract
{
    public class ContractModule : Module
    {

        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="contractSettings"></param>
        public static ContainerBuilder Load(ContainerBuilder builder)
        {
            builder.Register(c => Contract.GetInstance())
                .As<IContract>()
                .SingleInstance();
            return builder;
        }
    }
}
