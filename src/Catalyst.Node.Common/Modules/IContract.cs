using System;

namespace Catalyst.Node.Common.Modules
{
    public enum ContractType
    {
        Standard,
        External
    }

    public interface IContract
    {
        Guid Identity { get; set; }
        ContractType Type { get; set; }
        string Address { get; set; }
    }
}