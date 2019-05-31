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
        
        private readonly string scryptKeyStoreDocument = @"{
                ""crypto"" : {
                ""cipher"" : ""aes-128-ctr"",
                ""cipherparams"" : {
                    ""iv"" : ""83dbcc02d8ccb40e466191a123791e0e""
                },
                ""ciphertext"" : ""d172bf743a674da9cdad04534d56926ef8358534d458fffccd4e6ad2fbde479c"",
                ""kdf"" : ""scrypt"",
                ""kdfparams"" : {
                   ""dklen"" : 32,
                    ""n"" : 262144,
                    ""r"" : 1,
                    ""p"" : 8,
                    ""salt"" : ""ab0c7876052600dd703518d6fc3fe8984592145b591fc8fb5c6d43190334ba19""
                },
                ""mac"" : ""2103ac29920d71da29f15d75b4a16dbe95cfd7ff8faea1056c33131d846e3097""
                },
                ""id"" : ""3198bc9c-6672-5ab3-d995-4942343ae5b6"",
                ""version"" : 3
        }";
        
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
        public async void ShouldGenerateAccountAndCreateKeyStoreFileScrypt()
        {
            var catKey = _context.GeneratePrivateKey();

            var json = await _keystore.KeyStoreGenerate(catKey, "testPassword");
            var key = _keystore.KeyStoreDecrypt("testPassword", json);
            Assert.Equal(catKey.Bytes.RawBytes.ToHex(), key.ToHex(false));
        }
    }
}
