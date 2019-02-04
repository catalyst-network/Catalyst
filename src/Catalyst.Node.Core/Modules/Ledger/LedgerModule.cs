using Autofac;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Ledger
{
    public class LedgerModule : Module
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder Load(ContainerBuilder builder)
        {
            builder.Register(c => new Ledger())
                .As<ILedger>()
                .SingleInstance();
            return builder;
        }
    }
}
