using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    /// <summary>
    /// NSec specific private key wrapper
    /// </summary>
    public sealed class NSecPrivateKeyWrapper : IPrivateKey
    {

        private readonly Key _key;
        public NSecPrivateKeyWrapper(Key key)
        {
            _key = key;
        }
        public Key GetNSecFormatPrivateKey()
        {
            return _key;
        }

        public PublicKey GetNSecFormatPublicKey()
        {
            return _key.PublicKey;
        }
    } 
}