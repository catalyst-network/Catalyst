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
using Catalyst.Common.Config;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Keystore;
using Catalyst.Common.Registry;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.TestUtils;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Common.IntegrationTests.Modules.KeySigner
{
    public sealed class KeySignerIntegrationTests : FileSystemBasedTest
    {
        public KeySignerIntegrationTests(ITestOutputHelper output) : base(output)
        {
            var multiAlgo = Substitute.For<IMultihashAlgorithm>();
            multiAlgo.ComputeHash(Arg.Any<byte[]>()).Returns(new byte[32]);
            var addressHelper = new AddressHelper(multiAlgo);

            var logger = Substitute.For<ILogger>();

            var testPasswordReader = new TestPasswordReader();

            var cryptoContext = new CryptoContext(new CryptoWrapper());

            var keyStoreService = new KeyStoreServiceWrapped(cryptoContext);

            var keystore = new LocalKeyStore(testPasswordReader, cryptoContext, keyStoreService, FileSystem, logger, addressHelper);

            var keyRegistry = new KeyRegistry();

            _keySigner = new Common.Modules.KeySigner.KeySigner(keystore, cryptoContext, keyRegistry);
        }

        private readonly IKeySigner _keySigner;

        private void Ensure_A_KeyStore_File_Exists()
        {
            string json = @"""{""crypto"":{""cipher"":""aes-128-ctr"",""ciphertext"":""58e1617da38fc002816268967fea4d8d2f1dd51c8b638de5265bf06d781226a5""
                            ,""cipherparams"":{""iv"":""45f6c68c2ac3ad38f02aea8f3c928d2c""},""kdf"":""scrypt"",""mac"":""00bec3c8eb4988e9603105066cf297d7
                            4b67745ac5f7d749989344cfa4ee4b71"",""kdfparams"":{""n"":""262144,""r"":""1,""p"":""8,""dklen"":32,""salt"":""2a03d9840dec04e0
                            1538df649f61958be4015a97f14b765ec0a46feed88cc5f4""}},""id"":""b4b82bc3-a495-49cd-b3bc-e022f936e6ff"",""address"":""987080731d
                            e5a56833d2edc37458a53e3fec68cd"",""version"":3}";
            FileSystem.WriteTextFileToCddSubDirectoryAsync(KeyRegistryKey.DefaultKey.Name, Constants.KeyStoreDataSubDir, json);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void KeySigner_Can_Sign_If_There_Is_No_Keystore_File()
        {
            _keySigner.Sign(Encoding.UTF8.GetBytes("sign this plz"));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void KeySigner_Can_Sign_If_There_Is_An_Existing_Keystore_File()
        {
            Ensure_A_KeyStore_File_Exists();
            _keySigner.Sign(Encoding.UTF8.GetBytes("sign this plz"));
        }
    }
}
