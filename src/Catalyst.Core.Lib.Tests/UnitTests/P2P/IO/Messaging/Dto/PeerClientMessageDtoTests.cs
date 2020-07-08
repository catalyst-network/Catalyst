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
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Core.Lib.P2P.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Messaging.Dto
{
    public sealed class PeerClientMessageDtoTests
    {
        [Test]
        public void Can_Create_Dto_For_IPPN_Message()
        {
            var pingRequest = new PingRequest();
            var pid = MultiAddressHelper.GetAddress("sender");
            var dto = new PeerClientMessageDto(pingRequest,
                pid,
                Substitute.For<ICorrelationId>()
            );

            dto.Message.Should().Be(pingRequest);
            dto.Sender.Should().Be(pid);
        }

        [Test]
        public void Throws_Exception_For_Non_IPPN_Message()
        {
            var rpcMessage = new VersionRequest();

            Assert.Throws<ArgumentException>(() =>
            {
                // ReSharper disable once ObjectCreationAsStatement
                new PeerClientMessageDto(rpcMessage,
                    MultiAddressHelper.GetAddress("sender"),
                    Substitute.For<ICorrelationId>()
                );
            });
        }
    }
}
