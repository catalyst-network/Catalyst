using Autofac;

namespace Catalyst.Node.Modules.Core.Ledger
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