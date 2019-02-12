using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public sealed class NSecKeyWrapper : KeyWrapper<Key>, IKey{
        

        public NSecKeyWrapper(Key key) : base(key){
            
        }
        public Key GetNSecFormatKey()
        {
            return this.Key;
        }

        public PublicKey GetNSecFormatPublicKey()
        {
            return this.Key.PublicKey;
        }
    } 
}