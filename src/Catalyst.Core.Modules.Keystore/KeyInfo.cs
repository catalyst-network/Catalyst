using Catalyst.Abstractions.Cryptography;
using MultiFormats;

namespace Catalyst.Core.Modules.Keystore
{
    class KeyInfo : IKey
    {
        public string Name { get; set; }
        public MultiHash Id { get; set; }
    }
}
