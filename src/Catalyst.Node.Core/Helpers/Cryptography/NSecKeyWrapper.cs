using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public sealed class NSecKeyWrapper : IKey{
        
        private readonly Key _key;

        public NSecKeyWrapper(Key key){
            _key=key;
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