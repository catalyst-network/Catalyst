using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.UnitTests.TestUtils
{
    public class TestKeySigner : IKeySigner
    {
        public IKeyStore KeyStore => throw new NotImplementedException();

        public ICryptoContext CryptoContext => throw new NotImplementedException();

        public void ExportKey()
        {
            throw new NotImplementedException();
        }

        public void ReadPassword() { throw new NotImplementedException(); }
        public void GenerateNewKey() { throw new NotImplementedException(); }
        public string GetPublicKey() { return "z8Y55Tc3kESZJTtBupapV7WSmMrRPDf7PRzZJiWp6RsS1"; }

        public ISignature Sign(byte[] data)
        {
            return new Signature(new byte[64]);
        }

        public bool Verify(AnySigned anySigned) { return true; }
    }
}
