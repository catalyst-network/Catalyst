using Autofac;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Ledger
{
    public class LedgerModule : Module
    {

        public static ContainerBuilder Load(ContainerBuilder builder)
        {
            builder.Register(c => Ledger.GetInstance())
                .As<ILedger>()
                .SingleInstance();
            return builder;
        }
    }
}