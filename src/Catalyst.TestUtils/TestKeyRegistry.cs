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

using Catalyst.Common.Config;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class TestKeyRegistry
    {
        public static readonly string TestPrivateKey;
        public static readonly string TestPublicKey;
        
        static TestKeyRegistry()
        {
            TestPrivateKey = "1WG487E925TDYDG37DBBSHCVJQ7PT1TZK9TF2KS8CBPNBZAYQXQG";

            var cryptoContext = new CryptoContext(new CryptoWrapper());
            var fakePrivateKey = cryptoContext.PrivateKeyFromBytes(TestPrivateKey.KeyToBytes());
            TestPublicKey = fakePrivateKey.GetPublicKey().Bytes.KeyToString();
        }

        public static IKeyRegistry MockKeyRegistry()
        {
            var keyRegistry = Substitute.For<IKeyRegistry>();
            var cryptoContext = new CryptoContext(new CryptoWrapper());
            var fakePrivateKey = cryptoContext.PrivateKeyFromBytes(TestPrivateKey.KeyToBytes());
            keyRegistry.GetItemFromRegistry(KeyRegistryKey.DefaultKey).Returns(fakePrivateKey);
            keyRegistry.Contains(Arg.Any<byte[]>()).Returns(true);
            return keyRegistry;
        }
    }
}
