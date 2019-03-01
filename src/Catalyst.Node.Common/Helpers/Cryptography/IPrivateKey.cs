using NSec.Cryptography;

namespace Catalyst.Node.Common.Helpers.Cryptography
{
    /// <summary>
    /// Wrapper for private key.
    /// </summary>
    public interface IPrivateKey : IPublicKey{
        
        Key GetNSecFormatPrivateKey();
    }
}