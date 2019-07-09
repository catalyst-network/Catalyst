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

using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
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
            _keySigner = new Common.Modules.KeySigner.KeySigner(_keystore, new CryptoContext(_wrapper), _keyRegistry);
            privateKeyBytes = Util.ByteUtil.GenerateRandomByteArray(FFI.GetPrivateKeyLength());
        }

        private readonly IKeyStore _keystore;
        private readonly IKeyRegistry _keyRegistry;
        private readonly IWrapper _wrapper;
        private readonly IKeySigner _keySigner;
        private readonly byte[] privateKeyBytes;
        
        [Fact] 
        public void KeySigner_Can_Sign_If_Key_Exists_In_Registry()
        {
            _keyRegistry.GetItemFromRegistry(KeyRegistryKey.DefaultKey).Returns(new PrivateKey(privateKeyBytes));

            //_keySigner.Sign()
        }
    }
}
