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

using Catalyst.Cli.Commands;
using Catalyst.Cli.UnitTests.Helpers;
using Catalyst.Protocol;
using Catalyst.Protocol.Rpc.Node;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;

namespace Catalyst.Cli.UnitTests.Commands.Response
{
    public sealed class GetVersionResponseTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();

        [Fact]
        public void GetVersionResponse_Can_Get_Output()
        {
            //Arrange
            var versionResponse = new VersionResponse {Version = "1.2.3.4"};
            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);
            var getVersionCommand = new GetVersionCommand(commandContext);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, versionResponse);

            _testScheduler.Start();

            //Assert
            commandContext.UserOutput.Received(1).WriteLine(versionResponse.ToJsonString());
        }
    }
}
