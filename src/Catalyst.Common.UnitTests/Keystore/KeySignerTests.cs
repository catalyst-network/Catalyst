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

using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.KeyStore;
using Catalyst.Common.Modules.KeySigner;
using Catalyst.Common.UnitTests.TestUtils;
using FluentAssertions.Common;
using NSubstitute;
using Serilog;
using System.IO;
using System.Security;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Inbound;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels.Embedded;
using Google.Protobuf;
using Nethereum.KeyStore.Crypto;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.UnitTests.Keystore
{
    public sealed class KeySignerTests : FileSystemBasedTest
    {
        private const string Password = "Password";
        private readonly IPasswordReader _passwordReader;
        private readonly IKeySigner _keySigner;
        private readonly IKeySignerInitializer _keySignerInitializer;

        public KeySignerTests(ITestOutputHelper output) : base(output)
        {
            var logger = Substitute.For<ILogger>();
            _passwordReader = Substitute.For<IPasswordReader>();
            
            var secure = new SecureString();
            foreach (var c in Password)
            {
                secure.AppendChar(c);
            }

            _passwordReader.ReadSecurePassword(Arg.Any<string>()).Returns(secure);

            ICryptoContext context = new RustCryptoContext();
            IKeyStoreWrapper keyStoreWrapper = new KeyStoreServiceWrapped();
            IKeyStore keyStore = new LocalKeyStore(context, keyStoreWrapper, FileSystem, logger);
            _keySignerInitializer = new KeySignerInitializer(_passwordReader, keyStore, logger);
            _keySigner = new KeySigner(Substitute.For<IUserOutput>(),
                keyStore, context, _keySignerInitializer);
            _keySigner.ReadPassword();
        }

        [Fact]
        public void Test_Key_Signer_Initializer_Password()
        {
            _keySignerInitializer.Password.IsSameOrEqualTo(Password);
        }

        [Fact]
        public void Decrypt_Key_Signer_With_Invalid_Password_Throws_Error()
        {
            _passwordReader.ReadSecurePassword(Arg.Any<string>()).Returns(new SecureString());
            Assert.Throws<DecryptionException>(() => _keySigner.ReadPassword());
        }

        [Fact]
        public void Decrypt_Key_Signer_With_Valid_Password()
        {
            Assert.NotNull(_keySigner.GetPublicKey());
        }

        [Fact]
        public void Test_Key_Store_Created()
        {
            Assert.True(
                File.Exists(
                    Path.Combine(FileSystem.GetCatalystHomeDir().ToString(), Constants.DefaultKeyStoreFile)
                )
            );
        }

        [Fact]
        public void KeySigner_Can_Sign_Message() { Get_Signed_Message(); }
        
        [Fact]
        public void KeySigner_Can_Verify_Message()
        {
            AnySigned signed = Get_Signed_Message();
            _keySigner.Verify(signed).IsSameOrEqualTo(true);
        }

        private AnySigned Get_Signed_Message()
        {
            EmbeddedChannel channel = new EmbeddedChannel(new SignatureDuplexHandler(_keySigner));
            channel.WriteOutbound(new PingRequest().ToAnySigned(PeerIdHelper.GetPeerId(_keySigner.GetPublicKey())));
            var anySigned = (AnySigned) channel.OutboundMessages.Dequeue();

            Assert.NotNull(anySigned.Signature);
            Assert.Equal(64, anySigned.Signature.Length);
            Assert.NotEqual(ByteString.Empty, anySigned.Signature);
            return anySigned;
        }
    }
}
