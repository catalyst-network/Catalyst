#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
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
