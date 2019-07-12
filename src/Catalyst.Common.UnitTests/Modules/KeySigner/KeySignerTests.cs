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
using System.Linq;
using System.Text;
using Catalyst.Common.Config;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ReceivedExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Catalyst.Common.UnitTests.Modules.KeySigner
{
    public sealed class KeySignerTests
    {
        public KeySignerTests()
        {
            _keystore = Substitute.For<IKeyStore>();
            _keyRegistry = Substitute.For<IKeyRegistry>();
            _wrapper = Substitute.For<IWrapper>();
            
            var signatureBytes = ByteUtil.GenerateRandomByteArray(FFI.GetSignatureLength());
            var publicKeyBytes = ByteUtil.GenerateRandomByteArray(FFI.GetPublicKeyLength());
            _signature = new Signature(signatureBytes, publicKeyBytes);
            var privateKeyBytes = ByteUtil.GenerateRandomByteArray(FFI.GetPrivateKeyLength());
            var _privateKey = new PrivateKey(privateKeyBytes);
            _keystore.KeyStoreDecrypt(default).ReturnsForAnyArgs(_privateKey);

            _wrapper.StdSign(default, default).ReturnsForAnyArgs(_signature);
        }

        private readonly IKeyStore _keystore;
        private readonly IKeyRegistry _keyRegistry;
        private readonly IWrapper _wrapper;
        private IKeySigner _keySigner;
        private readonly ISignature _signature;
        private readonly IPrivateKey _privateKey;
        

        [Fact]
        public void On_Init_KeySigner_Can_Retrieve_Key_From_KeyStore_If_Key_Doesnt_Initially_Exist_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs((IPrivateKey) null);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(false);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            _keySigner = new Common.Modules.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryKey>());
            _keyRegistry.ReceivedWithAnyArgs(1).AddItemToRegistry(default, default);
        }

        [Fact]
        public void On_Init_KeySigner_Can_Generate_Default_Key_If_Key_No_KeyStore_File_Exists()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs((IPrivateKey) null);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(false);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            _keystore.KeyStoreDecrypt(default).ReturnsForAnyArgs((IPrivateKey) null);

            _keySigner = new Common.Modules.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryKey>());
            _keystore.Received(1).KeyStoreGenerateAsync(Arg.Any<KeyRegistryKey>());
            _keyRegistry.ReceivedWithAnyArgs(1).AddItemToRegistry(default, default);
        }


        [Fact] 
        public void KeySigner_Can_Sign_If_Key_Exists_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(default).ReturnsForAnyArgs(_privateKey);
            _keyRegistry.RegistryContainsKey(default).ReturnsForAnyArgs(true);
            _keyRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            _keySigner = new Common.Modules.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);

            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);
            _keystore.ClearReceivedCalls();
            _keyRegistry.ClearReceivedCalls();

            var sig = _keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"));

            _keyRegistry.ReceivedWithAnyArgs(1).GetItemFromRegistry(default);
            _keystore.Received(0).KeyStoreDecrypt(Arg.Any<KeyRegistryKey>());
            _keystore.Received(0).KeyStoreGenerateAsync(Arg.Any<KeyRegistryKey>());
            _keyRegistry.ReceivedWithAnyArgs(0).AddItemToRegistry(default, default);
            
            Assert.Equal(_signature, sig);
        }

        [Fact] 
        public void KeySigner_Can_Sign_If_Key_Doesnt_Exists_In_Registry_But_There_Is_A_Keystore_File()
        {
            _keyRegistry.GetItemFromRegistry(KeyRegistryKey.DefaultKey).Returns(null, _privateKey);

            var actualSignature = _keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"));
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryKey>());

            Assert.Equal(_signature, actualSignature);
        }

    }
}
