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
using System.Security.Authentication;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Keystore;
using Catalyst.Common.Types;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.TestUtils;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
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
        private readonly IPasswordReader _passwordReader;

        public LocalKeyStoreTests(ITestOutputHelper output) : base(output)
        {
            _context = new CryptoContext(new CryptoWrapper());

            var logger = Substitute.For<ILogger>();
            _passwordReader = Substitute.For<IPasswordReader>();
            _passwordReader.ReadSecurePassword(default, default)
               .ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("test password"));

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

        [Fact]
        public void KeyStore_Can_Generate_Key_And_Create_Keystore_File()
        {
            var privateKey = _keystore.KeyStoreGenerate(KeyRegistryTypes.DefaultKey);
            privateKey.Should().NotBe(null);
        }

        [Fact]
        public async Task KeyStore_Throws_Exception_On_Invalid_KeyStore_File()
        {
            await FileSystem.WriteTextFileToCddSubDirectoryAsync(KeyRegistryTypes.DefaultKey.Name, Constants.KeyStoreDataSubDir, "bad contents");
            Assert.Throws<Exception>(() => _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey));
        }

        [Fact]
        public void Keystore_Can_Create_Keystore_File_From_Provided_Key()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            _keystore.KeyStoreEncryptAsync(privateKey, KeyRegistryTypes.DefaultKey).Wait();
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);
            Assert.Equal(privateKey.Bytes, storedKey.Bytes);
        } 

        [Fact]
        public async Task Keystore_Can_Create_Keystore_File_From_Key_It_Generates()
        {
            var privateKey = _keystore.KeyStoreGenerate(KeyRegistryTypes.DefaultKey);

            await TaskHelper.WaitForAsync(() => FileSystem.DataFileExistsInSubDirectory(KeyRegistryTypes.DefaultKey.Name, Constants.CatalystDataDir),
                    TimeSpan.FromSeconds(3))
               .ConfigureAwait(false);
            
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);

            storedKey.Should().NotBe(null);
            Assert.Equal(privateKey.Bytes, storedKey.Bytes);
        }

        [Fact]
        public async Task Keystore_Throws_Exception_If_Password_Incorrect()
        {
            IPrivateKey privateKey = _context.GeneratePrivateKey();
            await _keystore.KeyStoreEncryptAsync(privateKey, KeyRegistryTypes.DefaultKey);
            await TaskHelper.WaitForAsync(
                () => FileSystem.DataFileExistsInSubDirectory(KeyRegistryTypes.DefaultKey.Name,
                    Constants.CatalystDataDir),
                TimeSpan.FromSeconds(3));
            _passwordReader.ReadSecurePassword(default, default)
               .ReturnsForAnyArgs(t => TestPasswordReader.BuildSecureStringPassword("a different test password"));
            
            Assert.Throws<AuthenticationException>(() => _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey));
        }

        [Fact]
        public void Keystore_Returns_Null_If_Key_File_Doesnt_Exist()
        {
            _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey).Should().Be(null);
        }
    }
}
