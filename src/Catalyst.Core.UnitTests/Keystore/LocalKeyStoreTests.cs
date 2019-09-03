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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Cryptography;
using Catalyst.Core.Keystore;
using Catalyst.Core.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.TestUtils;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.Keystore
{
    public sealed class LocalKeyStoreTests : FileSystemBasedTest
    {
        private readonly IFileSystem _fileSystem;
        private readonly IKeyStore _keystore;
        private readonly ICryptoContext _context;
        private readonly IPasswordManager _passwordManager;

        public LocalKeyStoreTests(ITestOutputHelper output) : base(output)
        {
            _fileSystem = Substitute.For<IFileSystem>();
            _context = new CryptoContext(new CryptoWrapper());

            var logger = Substitute.For<ILogger>();
            _passwordManager = Substitute.For<IPasswordManager>();
            _passwordManager.RetrieveOrPromptPassword(default)
               .ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("test password"));

            var multiAlgo = Substitute.For<IMultihashAlgorithm>();
            multiAlgo.ComputeHash(Arg.Any<byte[]>()).Returns(new byte[32]);

            var addressHelper = new AddressHelper(multiAlgo);

            _keystore = new LocalKeyStore(_passwordManager,
                _context,
                new KeyStoreServiceWrapped(_context),
                _fileSystem,
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
#pragma warning disable 1998
        public async Task KeyStore_Throws_Exception_On_Invalid_KeyStore_File()
#pragma warning restore 1998
        {
            _fileSystem.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns("bad contents");
            Assert.Throws<Exception>(() => _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey));
        }

        [Fact]
        public void Keystore_Can_Create_Keystore_File_From_Provided_Key()
        {
            string jsonKeyStore = null;
            _fileSystem.WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<string>(x => jsonKeyStore = x));

            var privateKey = _context.GeneratePrivateKey();
            _keystore.KeyStoreEncryptAsync(privateKey, KeyRegistryTypes.DefaultKey).Wait();

            _fileSystem.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns(jsonKeyStore);
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);
            privateKey.Bytes.Should().BeEquivalentTo(storedKey.Bytes);
        }

        [Fact]
        public async Task Keystore_Can_Create_Keystore_File_From_Key_It_Generates()
        {
            string jsonKeyStore = null;
            await _fileSystem.WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<string>(x => jsonKeyStore = x));

            var privateKey = await _keystore.KeyStoreGenerate(KeyRegistryTypes.DefaultKey);
            await _fileSystem.Received(1)
               .WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            _fileSystem.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns(jsonKeyStore);
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);

            storedKey.Should().NotBe(null);
            privateKey.Bytes.Should().BeEquivalentTo(storedKey.Bytes);
        }

        [Fact]
        public async Task Keystore_Throws_Exception_If_Password_Incorrect()
        {
            string jsonKeyStore = null;
            await _fileSystem.WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<string>(x => jsonKeyStore = x));

            var privateKey = _context.GeneratePrivateKey();
            await _keystore.KeyStoreEncryptAsync(privateKey, KeyRegistryTypes.DefaultKey);

            _fileSystem.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns(jsonKeyStore);

            _passwordManager.RetrieveOrPromptPassword(default)
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
