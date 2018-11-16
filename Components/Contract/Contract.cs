using System;

namespace ADL.Contract
{
    public enum ContractType {Standard, External}
    
    public enum StorageType {ADFS, IPFS, MaidSafe, TrustedServer}
    
    internal class Contract
    {
        public Guid Identity
        {
            get;
            set;
        }

        public ContractType Type
        {
            get;
            set;
        }
        
        public string Address
        {
            get;
            set;
        }
        
        public StorageType StorageMedium
        {
            get;
            set;
        }
    }
}
