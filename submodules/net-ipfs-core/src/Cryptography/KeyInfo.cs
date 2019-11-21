using Ipfs.Abstractions;
using MultiFormats;

namespace Ipfs.Core.Cryptography
{
    internal class KeyInfo : IKey
    {
        public string Name { get; set; }
        public MultiHash Id { get; set; }
    }
}
