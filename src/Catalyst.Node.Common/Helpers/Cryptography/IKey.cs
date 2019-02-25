using NSec.Cryptography;

namespace Catalyst.Node.Common.Cryptography
{
    /// <summary>
    /// Wrapper for private key.
    /// </summary>
    public interface IKey : IPublicKey{
        
        Key GetNSecFormatKey();
    }
}