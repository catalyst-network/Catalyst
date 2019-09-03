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

using System.Text;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Cryptography;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.KeySigner
{
    public sealed class KeySignerTests
    {
        public KeySignerTests()
        {
            _keystore = Substitute.For<IKeyStore>();
            _keyRegistry = Substitute.For<IKeyRegistry>();
            _wrapper = Substitute.For<IWrapper>();
            _signature = Substitute.For<ISignature>();
            _privateKey = Substitute.For<IPrivateKey>();

            _keystore.KeyStoreDecrypt(default).ReturnsForAnyArgs(_privateKey);

            _wrapper.StdSign(default, default, default).ReturnsForAnyArgs(_signature);

            _signingContext = new SigningContext();
        }

        private readonly IKeyStore _keystore;
        private readonly IKeyRegistry _keyRegistry;
        private readonly IWrapper _wrapper;
        private readonly ISignature _signature;
        private readonly IPrivateKey _privateKey;
        private readonly SigningContext _signingContext;

        [Fact]
        public void On_Init_KeySigner_Can_Retrieve_Key_From_KeyStore_If_Key_Doesnt_Initially_Exist_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs((IPrivateKey) null);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(false);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            var keySigner = new Core.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());
            _keyRegistry.ReceivedWithAnyArgs(1).AddItemToRegistry(default, default);
            keySigner.Should().NotBe(null);
        }

        [Fact]
        public void On_Init_KeySigner_Can_Generate_Default_Key_If_Key_No_KeyStore_File_Exists()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs((IPrivateKey) null);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(false);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            _keystore.KeyStoreDecrypt(default).ReturnsForAnyArgs((IPrivateKey) null);

            var keySigner = new Core.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());
            _keystore.Received(1).KeyStoreGenerate(Arg.Any<KeyRegistryTypes>());
            _keyRegistry.ReceivedWithAnyArgs(1).AddItemToRegistry(default, default);
            keySigner.Should().NotBe(null);
        }

        [Fact] 
        public void KeySigner_Can_Sign_If_Key_Exists_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs(_privateKey);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(true);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            var keySigner = new Core.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);

            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);
            _keystore.ClearReceivedCalls();
            _keyRegistry.ClearReceivedCalls();

            var actualSignature = keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"), _signingContext);

            _keyRegistry.ReceivedWithAnyArgs(1).GetItemFromRegistry(default);
            _keystore.Received(0).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());
            _keystore.Received(0).KeyStoreGenerate(Arg.Any<KeyRegistryTypes>());
            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);
            
            Assert.Equal(_signature, actualSignature);
        }

        [Fact] 
        public void KeySigner_Can_Sign_If_Key_Doesnt_Exists_In_Registry_But_There_Is_A_Keystore_File()
        {            
            var keySigner = new Core.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);            
            _keystore.ClearReceivedCalls();
            _keyRegistry.ClearReceivedCalls();

            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs(null, _privateKey);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(true);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            var actualSignature = keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"), _signingContext);

            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryTypes>());

            Assert.Equal(_signature, actualSignature);
        }
    }
}
