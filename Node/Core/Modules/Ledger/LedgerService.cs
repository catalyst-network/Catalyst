﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Helpers.Services;
 
 namespace ADL.Node.Core.Modules.Ledger
{
    public class LedgerService : AsyncServiceBase, ILedgerService
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
