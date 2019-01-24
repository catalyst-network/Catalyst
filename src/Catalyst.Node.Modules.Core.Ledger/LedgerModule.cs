using Autofac;

namespace Catalyst.Node.Modules.Core.Ledger
{
    public class LedgerModule : ModuleBase, ILedgerService
    {
        private readonly ILedger Ledger;
        private ILedgerSettings LedgerSettings;

        /// <summary>
        /// </summary>
        public LedgerModule(ILedger ledger, ILedgerSettings ledgerSettings)
        {
            Ledger = ledger;
            LedgerSettings = ledgerSettings;
        }

        /// <summary>
        ///     Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public ILedger GetImpl()
        {
            return Ledger;
        }
    }
}