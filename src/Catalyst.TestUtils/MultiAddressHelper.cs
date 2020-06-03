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

using System.Net;
using System.Text;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using MultiFormats;
using NSubstitute;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Catalyst.TestUtils
{
    public static class MultiAddressHelper
    {
        private static ICryptoContext ffiWrapper = new FfiWrapper();
        public static MultiAddress GetAddress(byte[] publicKey = null,
            IPAddress ipAddress = null,
            int port = 12345)
        {
            if (publicKey == null || publicKey.Length < ffiWrapper.PrivateKeyLength)
            {
                var g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");

                var random = SecureRandom.GetInstance("SHA1PRNG", false);
                if (publicKey != null)
                {
                    random.SetSeed(publicKey);
                }

                g.Init(new Ed25519KeyGenerationParameters(random));
                var keyPair = g.GenerateKeyPair();
                publicKey = ((Ed25519PublicKeyParameters) keyPair.Public).GetEncoded();
            }

            var address = new MultiAddress($"/ip4/{ipAddress ?? IPAddress.Loopback}/tcp/{port}/ipfs/{publicKey.ToPeerId()}");
            return address;
        }

        public static MultiAddress GetAddress(string publicKeySeed,
            IPAddress ipAddress = null,
            int port = 12345)
        {
            return GetAddress(Encoding.UTF8.GetBytes(publicKeySeed), ipAddress, port);
        }

        public static MultiAddress GetAddress(string publicKey, string ipAddress, int port)
        {
            return GetAddress(publicKey, IPAddress.Parse(ipAddress), port);
        }

        public static IPeerSettings ToSubstitutedPeerSettings(this MultiAddress address)
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.Address.Returns(address);
            peerSettings.BindAddress.Returns(IPAddress.Parse(address.GetIpAddress().ToString()));
            peerSettings.Port.Returns((int) address.GetPort());
            peerSettings.PublicKey.Returns(address.GetPublicKey());
            return peerSettings;
        }
    }
}
