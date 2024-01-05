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
using System.Collections.Generic;
using System.Text;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Modules.KeySigner.Tests.UnitTests
{
    public sealed class KeySignerTests
    {
        private IKeyStore _keystore;
        private IKeyRegistry _keyRegistry;
        private ISignature _signature;
        private IPrivateKey _privateKey;
        private SigningContext _signingContext;
        private ICryptoContext _cryptoContext;

        [TearDown]
        public void Teardown()
        {
            _keyRegistry.Dispose();
        }

        [SetUp]
        public void Init()
        {
            _keystore = Substitute.For<IKeyStore>();
            _keyRegistry = Substitute.For<IKeyRegistry>();
            _signature = Substitute.For<ISignature>();
            _privateKey = Substitute.For<IPrivateKey>();
            _cryptoContext = new CryptoContext(_signature);

            _privateKey.Bytes.Returns(ByteUtil.GenerateRandomByteArray(32));

            _keystore.KeyStoreDecrypt(default).ReturnsForAnyArgs(_privateKey);

            _signingContext = new SigningContext();
        }

        [Test]
        public void On_Init_KeySigner_Can_Retrieve_Key_From_KeyStore_If_Key_Doesnt_Initially_Exist_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs((IPrivateKey) null);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(false);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            var keySigner = new KeySigner(_keystore, _cryptoContext, _keyRegistry);
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());
            _keyRegistry.ReceivedWithAnyArgs(1).AddItemToRegistry(default, default);
            keySigner.Should().NotBe(null);
        }

        [Test]
        public void On_Init_KeySigner_Can_Generate_Default_Key_If_Key_No_KeyStore_File_Exists()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs((IPrivateKey) null);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(false);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            _keystore.KeyStoreDecrypt(default).ReturnsForAnyArgs((IPrivateKey) null);

            var keySigner = new KeySigner(_keystore, _cryptoContext, _keyRegistry);
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());
            _keystore.Received(1)?.KeyStoreGenerateAsync(Arg.Any<NetworkType>(), Arg.Any<KeyRegistryTypes>());
            _keyRegistry.ReceivedWithAnyArgs(1).AddItemToRegistry(default, default);
            keySigner.Should().NotBe(null);
        }

        [Test] 
        public void KeySigner_Can_Sign_If_Key_Exists_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs(_privateKey);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(true);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            var keySigner = new KeySigner(_keystore, _cryptoContext, _keyRegistry);

            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);
            _keystore.ClearReceivedCalls();
            _keyRegistry.ClearReceivedCalls();

            var actualSignature = keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"), _signingContext);

            _keyRegistry.ReceivedWithAnyArgs(1).GetItemFromRegistry(default);
            _keystore.Received(0).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());
            _keystore.Received(0)?.KeyStoreGenerateAsync(Arg.Any<NetworkType>(), Arg.Any<KeyRegistryTypes>());
            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);
            
            Assert.Equals(_signature, actualSignature);
        }

        [Test] 
        public void KeySigner_Can_Sign_If_Key_Doesnt_Exists_In_Registry_But_There_Is_A_Keystore_File()
        {            
            var keySigner = new KeySigner(_keystore, _cryptoContext, _keyRegistry);            
            _keystore.ClearReceivedCalls();
            _keyRegistry.ClearReceivedCalls();

            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs(null, _privateKey);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(true);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);
            
            var actualSignature = keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"), _signingContext);

            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());

            Assert.Equals(_signature, actualSignature);
        }

        private sealed class CryptoContext : ICryptoContext
        {
            private readonly ISignature _signature;
            public CryptoContext(ISignature signature) { _signature = signature; }

            public int PrivateKeyLength { get; }
            public int PublicKeyLength { get; }
            public int SignatureLength { get; }
            public int SignatureContextMaxLength { get; }
            public IPrivateKey GeneratePrivateKey() { throw new NotImplementedException(); }
            public IPublicKey GetPublicKeyFromPrivateKey(IPrivateKey privateKey) { throw new NotImplementedException(); }
            public IPublicKey GetPublicKeyFromBytes(byte[] publicKeyBytes) { throw new NotImplementedException(); }
            public IPrivateKey GetPrivateKeyFromBytes(byte[] privateKeyBytes) { throw new NotImplementedException(); }
            public ISignature GetSignatureFromBytes(byte[] signatureBytes, byte[] publicKeyBytes) { throw new NotImplementedException(); }
            public byte[] ExportPrivateKey(IPrivateKey privateKey) { throw new NotImplementedException(); }
            public byte[] ExportPublicKey(IPublicKey publicKey) { throw new NotImplementedException(); }

            public ISignature Sign(IPrivateKey privateKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context) => _signature;
            public bool Verify(ISignature signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context) { throw new NotImplementedException(); }
            public bool BatchVerify(IList<ISignature> signatures, IList<byte[]> messages, ReadOnlySpan<byte> context) { throw new NotImplementedException(); }
        }
    }
}
