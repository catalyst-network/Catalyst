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
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Keystore;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.TestUtils;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.UnitTests.Keystore
{
    public sealed class LocalKeyStoreTests : FileSystemBasedTest 
    {
        private readonly IKeyStore _keystore;
        private readonly ICryptoContext _context;

        public LocalKeyStoreTests(ITestOutputHelper output) : base(output)
        {
            _context = new CryptoContext(new CryptoWrapper());

            var logger = Substitute.For<ILogger>();
            var passwordReader = new TestPasswordReader("testPassword");

            var multiAlgo = Substitute.For<IMultihashAlgorithm>();
            multiAlgo.ComputeHash(Arg.Any<byte[]>()).Returns(new byte[32]);

            var addressHelper = new AddressHelper(multiAlgo);

            _keystore = new LocalKeyStore(passwordReader,
                _context,
                new KeyStoreServiceWrapped(_context),
                FileSystem,
                logger,
                addressHelper);
        }

        private void Ensure_No_Keystore_File_Exists()
        {
            var directoryInfo = FileSystem.GetCatalystDataDir();
            if (directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }

            directoryInfo.Create();
            directoryInfo.EnumerateFiles().Should().BeEmpty();
        }

        [Fact]
        public void Should_Generate_Account_And_Create_KeyStore_File_Scrypt()
        {
            Ensure_No_Keystore_File_Exists();
            var catKey = _context.GeneratePrivateKey();

            _keystore.KeyStoreGenerateAsync(catKey, KeyRegistryKey.DefaultKey);
            var key = _keystore.KeyStoreDecrypt(KeyRegistryKey.DefaultKey);
            Assert.Equal(catKey.Bytes.RawBytes.ToHex(), key.Bytes.RawBytes.ToHex());
        }

        //Keystore_Can_Create_Keystore_File_From_Key_It_Generates
        //Keystore_Can_Create_Keystore_File_From_Provided_Key
        //Keystore_Returns_Key_If_Keystore_exists
        //Keystore_Doesn't_return_Key_If_Keystore_doesn't_exist
        //Keystore_Doesn't_return_Key_If_PasswordIncorrect_doesn't_exist
        //Overwrite??
    }
}
