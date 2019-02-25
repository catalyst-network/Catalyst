using NSec.Cryptography;

namespace Catalyst.Node.Common.Cryptography
{
    /// <summary>
    /// NSec specific private key wrapper
    /// </summary>
    public sealed class NSecKeyWrapper : IKey
    {

        private readonly Key _key;
        public NSecKeyWrapper(Key key)
        {
            _key = key;
        }
        public Key GetNSecFormatKey()
        {
            return _key;
        }

        public PublicKey GetNSecFormatPublicKey()
        {
            return _key.PublicKey;
        }
    } 
}