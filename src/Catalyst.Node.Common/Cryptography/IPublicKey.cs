using NSec.Cryptography;

namespace Catalyst.Node.Common.Cryptography
{
    /// <summary>
    /// Wrapper for public key.
    /// </summary>
    public interface IPublicKey
    {
        PublicKey GetNSecFormatPublicKey();

    }
}