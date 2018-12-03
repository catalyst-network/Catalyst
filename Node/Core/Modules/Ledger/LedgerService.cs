using ADL.Node.Core.Helpers.Services;
 
 namespace ADL.Node.Core.Modules.Ledger
{
    public class LedgerService : ServiceBase, ILedgerService
    {
            
        private ILedger Ledger;
        private ILedgerSettings LedgerSettings;
        
        /// <summary>
        /// 
        /// </summary>
        public LedgerService(ILedger ledger, ILedgerSettings ledgerSettings)
        {
            Ledger = ledger;
            LedgerSettings = ledgerSettings;
        }
    }
}
