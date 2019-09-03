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

using Catalyst.Core.Cryptography;
using Catalyst.Core.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Utils
{
    public class KeyUtilTest
    {
        [Fact]
        public void Can_Encode_And_Decode_Correctly()
        {
            var cryptoContext = new CryptoContext(new CryptoWrapper());
            var privateKey = cryptoContext.GeneratePrivateKey();
            var publicKey = privateKey.GetPublicKey();

            var publicKeyAfterEncodeDecode = publicKey.Bytes.KeyToString().KeyToBytes();
            publicKeyAfterEncodeDecode.Should().BeEquivalentTo(publicKey.Bytes);
        }
    }
}
