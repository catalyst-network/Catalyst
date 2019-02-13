using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    /// <summary>
    /// Wrapper for private key.
    /// </summary>
    public interface IKey : IPublicKey{
        
        Key GetNSecFormatKey();
    }
}