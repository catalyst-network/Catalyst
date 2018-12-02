using Autofac;
using Autofac.Core;

namespace ADL.Node.Core.Modules.Ledger
{
    public class LedgerModule : Module, IModule
    {
        public void Load(ContainerBuilder builder, ILedgerSettings ledgerSettings)
        {
            builder.Register(c => new LedgerService(c.Resolve<ILedger>(), ledgerSettings))
                .As<ILedgerService>()
                .InstancePerLifetimeScope();
        }
    }
}