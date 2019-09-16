using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Cryptography;

namespace Catalyst.TestUtils.Protocol
{
    public static class SignatureHelper
    {
        public static Signature GetSignature(byte[] signature = default, SigningContext signingContext = default)
        {
            var defaultedSignature = new Signature
            {
                RawBytes = signature?.ToByteString() ?? ByteUtil.GenerateRandomByteArray(FFI.SignatureLength).ToByteString(),
                SigningContext = signingContext ?? DevNetPeerSigningContext.Instance
            };
            return defaultedSignature;
        }
    }
}
