using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    /// <summary>
    /// Wrapper for public key.
    /// </summary>
    public interface IPublicKey
    {
        PublicKey GetNSecFormatPublicKey();

    }
}