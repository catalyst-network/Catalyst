using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public class NSecKeyWrapper : IKey{
        private Key _key;

        public NSecKeyWrapper(Key key){
            _key=key;
        }
        public Key GetNSecFormatKey()
        {
            return _key;
        }
    } 
}