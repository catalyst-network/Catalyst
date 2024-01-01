#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class TestKeyRegistry
    {
        public static readonly string TestPrivateKey = "ftqm5kpzpo7bvl6e53q5j6mmrjwupbbiuszpsopxvjodkkqqiusa";
        public static readonly string TestPublicKey; // = "qnb9bw3b2yj4hpjcmsvgp12bkwff313v9gaqb18atvwfpevrmmf0"

        static TestKeyRegistry()
        {
            var cryptoContext = new FfiWrapper();
            var fakePrivateKey = cryptoContext.GetPrivateKeyFromBytes(TestPrivateKey.KeyToBytes());
            TestPublicKey = fakePrivateKey.GetPublicKey().Bytes.KeyToString();
        }

        public static IKeyRegistry MockKeyRegistry()
        {
            var keyRegistry = Substitute.For<IKeyRegistry>();
            var cryptoContext = new FfiWrapper();
            var fakePrivateKey = cryptoContext.GetPrivateKeyFromBytes(TestPrivateKey.KeyToBytes());
            keyRegistry.GetItemFromRegistry(KeyRegistryTypes.DefaultKey).Returns(fakePrivateKey);
            keyRegistry.Contains(Arg.Any<byte[]>()).Returns(true);
            return keyRegistry;
        }
    }
}
