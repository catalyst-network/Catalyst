using Catalyst.Core.Lib.Cryptography.Proto;
using MultiFormats;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using ProtoBuf;
using System.IO;
using System.Threading;

namespace Catalyst.Core.Lib.Extensions
{
    public static class PeerIdExtensions
    {
        public static byte[] GetPublicKeyBytesFromPeerId(this byte[] peerIdBytes)
        {
            using (var ms = new MemoryStream(peerIdBytes))
            {
                ms.Position = 0;

                var publicKey = Serializer.Deserialize<PublicKey>(ms);
                using var aIn = new Asn1InputStream(publicKey.Data);
                var info = SubjectPublicKeyInfo.GetInstance(aIn.ReadObject());
                return info.PublicKeyData.GetBytes();
            }
        }
    }
}
