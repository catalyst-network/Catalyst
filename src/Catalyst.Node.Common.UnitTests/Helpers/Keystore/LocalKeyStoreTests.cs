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
using System.IO;
using System.Text;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.Keystore;
using Catalyst.Node.Common.Helpers.KeyStore;
using Catalyst.Node.Common.UnitTests.TestUtils;
using FluentAssertions;
using NSec.Cryptography;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Common.UnitTests.Helpers.Keystore
{
    public sealed class LocalKeyStoreTests : FileSystemBasedTest
    {
        private readonly IKeyStore _keystore;
        private readonly ICryptoContext _context;
        private readonly IKeyStoreWrapper _keyStoreService;
        private readonly NSecPrivateKeyWrapper _privateKey;
        private readonly byte[] _privateKeyBytes;
        private readonly string _fileName;
        private readonly string _password;

        public LocalKeyStoreTests(ITestOutputHelper output) : base(output)
        {
            //we cannot currently use a mock for CryptoContext because part of the interface uses the "in"
            //parameter modifier: https://github.com/castleproject/Core/issues/430
            _context = new NSecCryptoContext();

            var logger = Substitute.For<ILogger>();
            _keyStoreService = Substitute.For<IKeyStoreWrapper>();
            _keystore = new LocalKeyStore(_context, _keyStoreService, _fileSystem, logger);

            _privateKey = new NSecPrivateKeyWrapper(new Key(SignatureAlgorithm.Ed25519,
                new KeyCreationParameters
                {
                    ExportPolicy = KeyExportPolicies.AllowPlaintextExport
                }));
            _privateKeyBytes = _context.ExportPrivateKey(_privateKey);
            _fileName = "myKey.json";
            _password = "password123";
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void TestStoreAndRetrieveKey()
        {
            var fakeJsonData = "this is the json data to be stored on disk file";
            _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(_password, Arg.Any<byte[]>(), _fileName)
               .Returns(fakeJsonData);

            _keystore.StoreKey(_privateKey, _fileName, _password);

            _keyStoreService.DecryptKeyStoreFromFile(_password, Arg.Is<string>(s => s.EndsWith(_fileName)))
               .Returns(_privateKeyBytes);

            IPrivateKey retrievedKey = _keystore.GetKey(_fileName, _password);
            retrievedKey.Should().NotBeNull();

            // we can't export the key and test its value directly so we test it produces the same encryption for a given message
            var testMessage = Encoding.UTF8.GetBytes("test message");
            var testKeyBySigning = _context.Sign(retrievedKey, testMessage);
            var expectedSignedMessage = _context.Sign(_privateKey, testMessage);

            testKeyBySigning.Should().BeEquivalentTo(expectedSignedMessage, "the stored and retrieve key should be the same");

            var storedData = File.ReadAllText(Path.Combine(_fileSystem.GetCatalystHomeDir().FullName, _fileName));
            storedData.Should().Be(fakeJsonData);
        }

        [Fact]
        public void TestWrongPasswordStoreAndRetrieveKey()
        {
            _keystore.StoreKey(_privateKey, _fileName, _password);

            _keyStoreService.DecryptKeyStoreFromFile(Arg.Any<string>(), Arg.Is<string>(s => s.EndsWith(_fileName)))
               .Throws(new CustomNethereumException("wrong password"));

            string password2 = "incorrect password";
            Action action = () => _keystore.GetKey(_fileName, password2);
            action.Should().Throw<CustomNethereumException>("we should not be able to retrieve a key with the wrong password");
        }

        private sealed class CustomNethereumException : Exception
        {
            public CustomNethereumException(string message) : base(message) { }
        }
    }
}
