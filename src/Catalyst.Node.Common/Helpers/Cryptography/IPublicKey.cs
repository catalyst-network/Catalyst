using NSec.Cryptography;

namespace Catalyst.Node.Common.Helpers.Cryptography
{
    /// <summary>
    /// Wrapper for public key.
    /// </summary>
    public interface IPublicKey
    {
        PublicKey GetNSecFormatPublicKey();

    }
}