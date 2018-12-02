using System;

namespace ADL.Contract
{
    public enum StorageType { IPFS }
    public enum ContractType { Standard, External }
    
    public interface IContract
    {
        Guid Identity { get; set; }
        ContractType Type { get; set; }
        string Address { get; set; }
        StorageType StorageMedium { get; set; }
    }
}
