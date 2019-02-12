using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public interface IPublicKey
    {
        PublicKey GetNSecFormatPublicKey();

    }
}