using System;

namespace ADL.Node.Core.Modules.Contract
{
    public enum StorageType { Ipfs }
    public enum ContractType { Standard, External }
    
    public interface IContract
    {
        Guid Identity { get; set; }
        ContractType Type { get; set; }
        string Address { get; set; }
        StorageType StorageMedium { get; set; }
    }
}
