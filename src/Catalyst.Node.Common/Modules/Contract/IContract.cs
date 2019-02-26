using System;

namespace Catalyst.Node.Common.Modules.Contract
{
    public interface IContract
    {
        Guid Identity { get; set; }
        ContractType Type { get; set; }
        string Address { get; set; }
    }
}