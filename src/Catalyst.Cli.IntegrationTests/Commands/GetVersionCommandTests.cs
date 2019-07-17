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

using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    public sealed class GetVersionCommandTests : CliCommandTestsBase
    {
        public GetVersionCommandTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Cli_Can_Request_Node_Version()
        {
            var result = Shell.ParseCommand("getversion", NodeArgumentPrefix, ServerNodeName);
            result.Should().BeTrue();

            AssertSentMessageAndGetMessageContent<VersionRequest>();
        }
    }
}
