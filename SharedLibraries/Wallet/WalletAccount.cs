using System.Collections.Generic;
using ADL.Math;

namespace ADL.Wallet
{
    /*
     * An account is made up of a public/private keypair and address
     */
    public abstract class WalletAccount
    {
        public readonly UInt160 ScriptHash;
        
        public bool Lock
        {
            get;
            set;
        }
        
        public abstract bool HasKey
        {
            get;
        }

        public string Address
        {
            get;
            set;
        }
        
        public abstract KeyPair GetKey();
        
        protected WalletAccount(UInt160 scriptHash)
        {
            this.ScriptHash = scriptHash;
        }
    }
}