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
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Core;
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

            _wrapper.StdSign(default, default).ReturnsForAnyArgs(_signature);
        }

        private readonly IKeyStore _keystore;
        private readonly IKeyRegistry _keyRegistry;
        private readonly IWrapper _wrapper;
        private IKeySigner _keySigner;
        private readonly ISignature _signature;
        private readonly byte[] _privateKeyBytes = ByteUtil.GenerateRandomByteArray(FFI.GetPrivateKeyLength());
        
        [Fact] 
        public void KeySigner_Can_Sign_If_Key_Exists_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(KeyRegistryKey.DefaultKey).Returns(new PrivateKey(_privateKeyBytes));

            _keySigner = new Common.Modules.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            var sig = _keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"));
            _keystore.DidNotReceive().KeyStoreDecrypt(Arg.Any<KeyRegistryKey>());

            Assert.Equal(_signature, sig);
        }

        [Fact]
        public void KeySigner_Can_Sign_If_Not_Initialised_With_Key()
        {
            //Is this testing this??
            _keyRegistry.GetItemFromRegistry(KeyRegistryKey.DefaultKey).Returns(null, new PrivateKey(_privateKeyBytes));
            _keySigner = new Common.Modules.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            _keySigner.Sign(Encoding.UTF8.GetBytes("sign this please"));
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryKey>());

            Assert.Equal(_signature, _keySigner.Sign(Encoding.UTF8.GetBytes("sign this please")));
        }

        [Fact]
        public void KeySigner_Can_Initialise_If_Key_Doesnt_Initially_Exist_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(KeyRegistryKey.DefaultKey).Returns(null, new PrivateKey(_privateKeyBytes));
            _keySigner = new Common.Modules.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            _keystore.Received(1).KeyStoreDecrypt(Arg.Any<KeyRegistryKey>());
        }

        //KeySigner_Can_Initialise_With_Key_If_KeyStore_File_Exists()

        //KeySigner_Can_Initialise_With_Key_If_KeyStore_File_Doesnt_Exist()

        //KeySigner_Can_Sign_If_Initialised_With_Key()
    }
}
