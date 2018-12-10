using System;

namespace ADL.KeyStore
{
    public class InvalidKdfException : Exception
    {
        public InvalidKdfException(string kdf) : base("Invalid kdf:" + kdf)
        {
        }
    }
}