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

using System.Reactive.Threading.Tasks;
using System.Security.Authentication;
using Catalyst.Common.Config;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Keystore;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
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
        private readonly TestPasswordReader _passwordReader;

        public LocalKeyStoreTests(ITestOutputHelper output) : base(output)
        {
            _context = new CryptoContext(new CryptoWrapper());

            var logger = Substitute.For<ILogger>();
            _passwordReader = new TestPasswordReader("testPassword");

            var multiAlgo = Substitute.For<IMultihashAlgorithm>();
            multiAlgo.ComputeHash(Arg.Any<byte[]>()).Returns(new byte[32]);

            var addressHelper = new AddressHelper(multiAlgo);

            _keystore = new LocalKeyStore(_passwordReader,
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
        public void KeyStore_Can_Generate_Key_And_Create_Keystore_File()
        {
            Ensure_No_Keystore_File_Exists();
            
            var privateKey = _keystore.KeyStoreGenerateAsync(KeyRegistryKey.DefaultKey);
            privateKey.Should().NotBe(null);
        }

        [Fact]
        public void KeyStore_Throws_Exception_On_Invalid_KeyStore_File()
        {
            Ensure_No_Keystore_File_Exists();
            FileSystem.WriteFileToCddSubDirectoryAsync("bad_file", Constants.KeyStoreDataSubDir, "bad contents");
            Assert.Throws<AuthenticationException>(() =>_keystore.KeyStoreDecrypt(KeyRegistryKey.DefaultKey));
        }

        [Fact]
        public void Keystore_Can_Create_Keystore_File_From_Provided_Key()
        {
            Ensure_No_Keystore_File_Exists();

            IPrivateKey privateKey = _context.GeneratePrivateKey();
            _keystore.KeyStoreEncryptAsync(privateKey, KeyRegistryKey.DefaultKey).Wait();
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryKey.DefaultKey);
            Assert.Equal(privateKey.Bytes.RawBytes, storedKey.Bytes.RawBytes);
        } 

        [Fact]
        public void Keystore_Can_Create_Keystore_File_From_Key_It_Generates()
        {
            Ensure_No_Keystore_File_Exists();
            
            _keystore.KeyStoreGenerateAsync(KeyRegistryKey.DefaultKey).Wait();
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryKey.DefaultKey);

            storedKey.Should().NotBe(null);
        }

        [Fact]
        public void Keystore_Throws_Exception_If_Password_Incorrect()
        {
            Ensure_No_Keystore_File_Exists();

            IPrivateKey privateKey = _context.GeneratePrivateKey();
            _keystore.KeyStoreEncryptAsync(privateKey, KeyRegistryKey.DefaultKey).Wait();
            _passwordReader.ReadSecurePassword(default, default).ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("aDifferentPassword"));
            Assert.Throws<AuthenticationException>(() => _keystore.KeyStoreDecrypt(KeyRegistryKey.DefaultKey));
        }

        [Fact]
        public void Keystore_Returns_Null_If_Key_File_Doesnt_Exist()
        {
            Ensure_No_Keystore_File_Exists();

            _keystore.KeyStoreDecrypt(KeyRegistryKey.DefaultKey).Should().Be(null);
        }
    }
}
