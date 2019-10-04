using System.Collections.Generic;
using Ipfs;

namespace Catalyst.Abstractions.Hashing
{
    public interface IHashProvider
    {
        MultiHash ComputeMultiHash(IEnumerable<byte> content);
    }
}
