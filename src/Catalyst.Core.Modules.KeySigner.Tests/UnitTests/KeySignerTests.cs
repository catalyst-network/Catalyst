#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.Text;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol;
using Catalyst.Protocol.Cryptography;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Catalyst.Core.Modules.KeySigner.Tests.UnitTests
{
    public sealed class KeySignerTests
    {
        private IKeyApi _keyApi;
        private IKeyRegistry _keyRegistry;
        private IPrivateKey _privateKey;
        private SigningContext _signingContext;
        private ICryptoContext _cryptoContext;

        [SetUp]
        public void Init()
        {
            _keyApi = Substitute.For<IKeyApi>();
            _keyRegistry = Substitute.For<IKeyRegistry>();
            _privateKey = Substitute.For<IPrivateKey>();
            _cryptoContext = new FfiWrapper();

            var g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");
            SecureRandom random = new();
            g.Init(new Ed25519KeyGenerationParameters(random));
            var keyPair = g.GenerateKeyPair();

            _keyApi.GetPrivateKeyAsync(KeyRegistryTypes.DefaultKey.Name).Returns(keyPair.Private);
            _privateKey.Bytes.Returns(((Ed25519PrivateKeyParameters) keyPair.Private).GetEncoded());

            _signingContext = new SigningContext();
        }

        [Test]
        public void KeySigner_Can_Sign_If_Key_Exists_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs(_privateKey);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(true);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            KeySigner keySigner = new(_cryptoContext, Substitute.For<IKeyApi>(), _keyRegistry);

            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);
            _keyRegistry.ClearReceivedCalls();

            var content = Encoding.UTF8.GetBytes("sign this please");
            var actualSignature = keySigner.Sign(content, _signingContext);

            _keyRegistry.ReceivedWithAnyArgs(1).GetItemFromRegistry(default);
            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);

            using var pooled = _signingContext.SerializeToPooledBytes();
            var signature = _cryptoContext.Sign(_privateKey, content, pooled.Span);
            signature.PublicKeyBytes.Should().BeEquivalentTo(actualSignature.PublicKeyBytes);
            signature.SignatureBytes.Should().BeEquivalentTo(actualSignature.SignatureBytes);
        }

        [Test]
        public void KeySigner_Can_Sign_If_Key_Doesnt_Exists_In_Registry_But_There_Is_A_Keystore_File()
        {
            KeySigner keySigner = new(_cryptoContext, _keyApi, _keyRegistry);
            _keyRegistry.ClearReceivedCalls();

            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(false);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            var content = Encoding.UTF8.GetBytes("sign this please");
            var actualSignature = keySigner.Sign(content, _signingContext);

            _keyApi.Received(1).GetPrivateKeyAsync(KeyRegistryTypes.DefaultKey.Name);

            using var pooled = _signingContext.SerializeToPooledBytes();
            var signature = _cryptoContext.Sign(_privateKey, content, pooled.Span);
            signature.PublicKeyBytes.Should().BeEquivalentTo(actualSignature.PublicKeyBytes);
            signature.SignatureBytes.Should().BeEquivalentTo(actualSignature.SignatureBytes);
        }
    }
}
