#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.IntegrationTests.Commands
{
    public sealed class PeerReputationCommandTests : CliCommandTestsBase
    {
        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);
        }

        [Test]
        public void Cli_Can_Send_Peer_Reputation_Request()
        {
            var address = "/ip4/127.0.0.1/tcp/42066/ipfs/18n3naE9kBZoVvgYMV6saMZdwu2yu3QMzKa2BDkb5C5pcuhtrH1G9HHbztbbxA8tGmf4";

            var result = Shell.ParseCommand(
                "peerrep", NodeArgumentPrefix, ServerNodeName, "-a", address);
            result.Should().BeTrue();

            AssertSentMessageAndGetMessageContent<GetPeerReputationRequest>();
        }
    }
}
