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
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.KeyStore;
using Catalyst.Common.Modules.KeySigner;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using FluentAssertions;
using FluentAssertions.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.UnitTests.Keystore
{
    public sealed class KeySignerTests : FileSystemBasedTest
    {
        private const string Password = "Password";
        private readonly IPasswordReader _passwordReader;
        private readonly IKeySigner _keySigner;
        private readonly ILogger _logger;

        public KeySignerTests(ITestOutputHelper output) : base(output)
        {
            _passwordReader = Substitute.For<IPasswordReader>();
            _logger = Substitute.For<ILogger>();

            var secure = new SecureString();
            foreach (char c in Password)
            {
                secure.AppendChar(c);
            }

            _passwordReader.ReadSecurePassword(Arg.Any<string>()).Returns(secure);

            ICryptoContext context = new RustCryptoContext();
            IKeyStoreWrapper keyStoreWrapper = new KeyStoreServiceWrapped();
            IKeyStore keyStore = new LocalKeyStore(context, keyStoreWrapper, FileSystem, _logger);
            _keySigner = new KeySigner(Substitute.For<IUserOutput>(),
                keyStore, context, new KeySignerInitializer(_passwordReader, keyStore, _logger));
            _keySigner.GenerateNewKey();
        }

        [Fact]
        public void Test_Key_Signer_Initializer_Password()
        {
            IKeySignerInitializer initializer = new KeySignerInitializer(_passwordReader, Substitute.For<IKeyStore>(), _logger);
            initializer.ReadPassword(Substitute.For<IKeySigner>());
            initializer.Password.IsSameOrEqualTo(Password);
        }
    }
}
