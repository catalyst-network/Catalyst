using System.Collections.Generic;

namespace Catalyst.Abstractions.Hashing
{
    public interface IHashProvider
    {
        byte[] ComputeHash(IEnumerable<byte> content);

        bool IsValidHash(IEnumerable<byte> content);

        string AsBase32(IEnumerable<byte> content);

        byte[] ComputeToBase32(IEnumerable<byte> content);

        byte[] GetBase32HashBytes(string hash);
    }
}
