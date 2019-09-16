using System.Collections.Generic;

namespace Catalyst.Abstractions.Hashing
{
    public interface IHashProvider
    {
        byte[] ComputeRawHash(IEnumerable<byte> content);

        bool IsValidHash(IEnumerable<byte> content);

        string ComputeBase32(IEnumerable<byte> content);
        
        byte[] GetBase32EncodedBytes(string hash);
    }
}
