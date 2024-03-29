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

using System.IO;
using System.Threading.Tasks;
using Lib.P2P.Cryptography;
using Lib.P2P.SecureCommunication;

namespace Lib.P2P.Tests.SecureCommunication
{
    public class Psk1ProtectorTest
    {
        [Test]
        public async Task Protect()
        {
            var psk = new PreSharedKey().Generate();
            var protector = new Psk1Protector {Key = psk};
            var connection = new PeerConnection {Stream = Stream.Null};
            var protectedStream = await protector.ProtectAsync(connection);
            Assert.That(protectedStream, Is.TypeOf(typeof(Psk1Stream)));
        }
    }
}
