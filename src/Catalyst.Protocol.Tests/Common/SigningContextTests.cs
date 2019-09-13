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

using Catalyst.Protocol.Wire;
using FluentAssertions;
using Xunit;

namespace Catalyst.Protocol.Tests.Common
{
    public class SigningContextTests
    {
        [Fact]
        public void Signing_Context_Should_Contain_Default_Values()
        {
            var signingContext = new SigningContext();
            signingContext.Network.Should().BeEquivalentTo(Network.Devnet);
            signingContext.SignatureType.Should().BeEquivalentTo(SignatureType.TransactionPublic);
        }

        [Fact]
        public void Can_Create_Signing_Context_With_Non_Default_Values()
        {
            var signingContext = new SigningContext
            {
                Network = Network.Mainnet,
                SignatureType = SignatureType.ProtocolRpc
            };

            signingContext.Network.Should().BeEquivalentTo(Network.Mainnet);
            signingContext.SignatureType.Should().BeEquivalentTo(SignatureType.ProtocolRpc);
        }
    }
}
