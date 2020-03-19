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
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using FluentAssertions;
using MultiFormats.Registry;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Catalyst.Core.Modules.Keystore.Tests.IntegrationTests
{
    public sealed class LocalKeyStoreTests : FileSystemBasedTest
    {
        private readonly IFileSystem _fileSystem;
        private readonly IKeyStore _keystore;
        private readonly ICryptoContext _context;
        private readonly IPasswordManager _passwordManager;

        public LocalKeyStoreTests(TestContext output) : base(output)
        {
            _fileSystem = Substitute.For<IFileSystem>();
            _context = new FfiWrapper();

            var logger = Substitute.For<ILogger>();
            _passwordManager = Substitute.For<IPasswordManager>();
            _passwordManager.RetrieveOrPromptPassword(default)
               .ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("test password"));

            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _keystore = new LocalKeyStore(_passwordManager,
                _context,
                _fileSystem,
                hashProvider,
                logger);
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void KeyStore_Can_Generate_Key_And_Create_Keystore_File()
        {
            var privateKey = _keystore.KeyStoreGenerateAsync(NetworkType.Devnet, KeyRegistryTypes.DefaultKey);
            privateKey.Should().NotBe(null);
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void KeyStore_Throws_Exception_On_Invalid_KeyStore_File()
        {
            _fileSystem.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns("bad contents");
            Assert.Throws<Exception>(() => _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey));
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public async Task Keystore_Can_Create_Keystore_File_From_Provided_Key()
        {
            string jsonKeyStore = null;
            _fileSystem?.WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<string>(x => jsonKeyStore = x));

            var privateKey = _context.GeneratePrivateKey();
            await _keystore.KeyStoreEncryptAsync(privateKey, NetworkType.Devnet, KeyRegistryTypes.DefaultKey);

            _fileSystem?.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns(jsonKeyStore);
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);
            privateKey.Bytes.Should().BeEquivalentTo(storedKey.Bytes);
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public async Task Keystore_Can_Create_Keystore_File_From_Key_It_Generates()
        {
            string jsonKeyStore = null;
            await _fileSystem.WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<string>(x => jsonKeyStore = x));

            var privateKey = await _keystore.KeyStoreGenerateAsync(NetworkType.Devnet, KeyRegistryTypes.DefaultKey);
            await _fileSystem.Received(1)
               .WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            _fileSystem.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns(jsonKeyStore);
            var storedKey = _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey);

            storedKey.Should().NotBe(null);
            privateKey.Bytes.Should().BeEquivalentTo(storedKey.Bytes);
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public async Task Keystore_Throws_Exception_If_Password_Incorrect()
        {
            string jsonKeyStore = null;
            await _fileSystem.WriteTextFileToCddSubDirectoryAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<string>(x => jsonKeyStore = x));

            var privateKey = _context.GeneratePrivateKey();
            await _keystore.KeyStoreEncryptAsync(privateKey, NetworkType.Devnet, KeyRegistryTypes.DefaultKey);

            _fileSystem.ReadTextFromCddSubDirectoryFile(Arg.Any<string>(), Arg.Any<string>())
               .Returns(jsonKeyStore);

            _passwordManager.RetrieveOrPromptPassword(default)
               .ReturnsForAnyArgs(t => TestPasswordReader.BuildSecureStringPassword("a different test password"));

            Assert.Throws<AuthenticationException>(() => _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey));
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Keystore_Returns_Null_If_Key_File_Doesnt_Exist()
        {
            _keystore.KeyStoreDecrypt(KeyRegistryTypes.DefaultKey).Should().Be(null);
        }
    }
}
