#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Catalyst.Cli.Commands;
using Catalyst.Cli.Tests.UnitTests.Helpers;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Rpc.Node;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.UnitTests.Commands.Response
{
    [TestFixture]
    public sealed class GetDeltaResponseTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();
        private readonly ILogger _logger;

        public GetDeltaResponseTests() { _logger = Substitute.For<ILogger>(); }

        [Test]
        public void GetDeltaResponse_Can_Get_Output()
        {
            //Arrange
            var deltaResponse = new GetDeltaResponse {Delta = new Delta()};
            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);
            new GetDeltaCommand(commandContext, _logger);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, deltaResponse);

            _testScheduler.Start();

            //Assert
            commandContext.UserOutput.Received(1).WriteLine(deltaResponse.Delta.ToJsonString());
        }

        [Test]
        public void GetDeltaResponse_Error_On_Null_Delta()
        {
            //Arrange
            var deltaResponse = new GetDeltaResponse();
            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);
            new GetDeltaCommand(commandContext, _logger);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, deltaResponse);

            _testScheduler.Start();

            //Assert
            commandContext.UserOutput.Received(1).WriteLine(GetDeltaCommand.UnableToRetrieveDeltaMessage);
        }
    }
}
