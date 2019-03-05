namespace Catalyst.Node.Common.Helpers.Cryptography
{
    public class Signature
    {
        public byte[] Bytes{ get; private set; }

        public Signature(byte[] bytes) { Bytes = bytes; }

    }
}