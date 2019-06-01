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
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.KeyStore;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.UnitTests.Keystore
{
    public sealed class LocalKeyStoreTests : ConfigFileBasedTest
    {
        private readonly IKeyStore _keystore;
        private readonly ICryptoContext _context;

        public LocalKeyStoreTests(ITestOutputHelper output) : base(output)
        {
            _context = new RustCryptoContext();

            var logger = Substitute.For<ILogger>();
            var passwordReader = new TestPasswordReader("testPassword");

            _keystore = new LocalKeyStore(passwordReader,
                _context,
                new KeyStoreServiceWrapped(),
                FileSystem,
                logger,
                new AddressHelper(new BLAKE2B_256())
            );
        }

        [Fact]
        public void ShouldGenerateAccountAndCreateKeyStoreFileScrypt()
        {
            var catKey = _context.GeneratePrivateKey();

            var json = _keystore.KeyStoreGenerate(catKey, "testPassword").GetAwaiter().GetResult();
            var key = _keystore.KeyStoreDecrypt("testPassword", json);
            Assert.Equal(catKey.Bytes.RawBytes.ToHex(), key.ToHex(false));
        }
    }
}
