using System;

namespace ADL.KeyStore.Crypto
{
    public class DecryptionException : Exception
    {
        internal DecryptionException(string msg) : base(msg)
        {
        }
    }
}