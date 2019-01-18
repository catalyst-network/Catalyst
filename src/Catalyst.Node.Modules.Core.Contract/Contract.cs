using System;

namespace Catalyst.Node.Modules.Core.Contract
{
    public class Contract : IContract
    {
        public Guid Identity { get; set; }
        public ContractType Type { get; set; }
        public string Address { get; set; }
        public StorageType StorageMedium { get; set; }
    }
}