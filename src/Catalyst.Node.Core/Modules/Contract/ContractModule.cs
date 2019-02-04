using Autofac;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Contract
{
    public class ContractModule : Module
    {

        /// <summary>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="contractSettings"></param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new Contract())
                .As<IContract>()
                .SingleInstance();
        }
    }
}
