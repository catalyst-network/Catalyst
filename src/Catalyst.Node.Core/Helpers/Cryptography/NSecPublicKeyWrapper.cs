using System;
using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public sealed class NSecPublicKeyWrapper : KeyWrapper<PublicKey>, IPublicKey{
        
        public NSecPublicKeyWrapper(PublicKey key) : base(key){}
        public NSecPublicKeyWrapper() : base(){}
        
        public PublicKey GetNSecFormatPublicKey()
        {
            return this.Key;
        }
    } 
}