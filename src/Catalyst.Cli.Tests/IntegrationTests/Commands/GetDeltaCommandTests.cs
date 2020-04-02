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

using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using MultiFormats;
using MultiFormats.Registry;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.IntegrationTests.Commands
{
    public sealed class GetDeltaCommandTests : CliCommandTestsBase
    {
        public GetDeltaCommandTests() : base(TestContext.CurrentContext) { }

        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);
        }

        [Test]
        public void Cli_Can_Request_Node_Info()
        {
            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            var hash = hashProvider.ComputeUtf8MultiHash("hello").ToCid();

            var result = Shell.ParseCommand("getdelta", "-h", hash, NodeArgumentPrefix, ServerNodeName);
            result.Should().BeTrue();

            var request = AssertSentMessageAndGetMessageContent<GetDeltaRequest>();
            MultiBase.Encode(request.DeltaDfsHash.ToByteArray(), "base32").Should().Be(hash);
        }
    }
}
