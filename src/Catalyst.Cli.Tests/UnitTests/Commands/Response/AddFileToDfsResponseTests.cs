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

using Catalyst.Abstractions.Types;
using Catalyst.Cli.Commands;
using Catalyst.Cli.Tests.UnitTests.Helpers;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Cli.Tests.UnitTests.Commands.Response
{
    [TestFixture]
    public sealed class AddFileToDfsResponseTests
    {
        private readonly TestScheduler _testScheduler = new TestScheduler();
        private readonly ILogger _logger;

        public AddFileToDfsResponseTests() { _logger = Substitute.For<ILogger>(); }

        [Test]
        public void AddFileToDfsResponse_Failed_Can_Get_Output()
        {
            //Arrange
            var addFileToDfsResponse = new AddFileToDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodeTypes.Failed.Id)
            };

            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);
            new AddFileCommand(null, commandContext, _logger);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, addFileToDfsResponse);

            _testScheduler.Start();

            //Assert
            commandContext.UserOutput.Received(1).WriteLine("File transfer completed, Response: " +
                FileTransferResponseCodeTypes.Failed.Name + " Dfs Hash: " +
                addFileToDfsResponse.DfsHash);
        }

        [Test]
        public void AddFileToDfsResponse_Finished_Can_Get_Output()
        {
            //Arrange
            var addFileToDfsResponse = new AddFileToDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodeTypes.Finished.Id)
            };

            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);
            new AddFileCommand(null, commandContext, _logger);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, addFileToDfsResponse);

            _testScheduler.Start();

            //Assert
            commandContext.UserOutput.Received(1).WriteLine("File transfer completed, Response: " +
                FileTransferResponseCodeTypes.Finished.Name +
                " Dfs Hash: " + addFileToDfsResponse.DfsHash);
        }

        [Test]
        public void AddFileToDfsResponse_No_Response_Codes()
        {
            //Arrange
            var addFileToDfsResponse = new AddFileToDfsResponse();
            var commandContext = TestCommandHelpers.GenerateCliResponseCommandContext(_testScheduler);
            new AddFileCommand(null, commandContext, _logger);

            //Act
            TestCommandHelpers.GenerateResponse(commandContext, addFileToDfsResponse);

            _testScheduler.Start();

            //Assert
            commandContext.UserOutput.Received(1).WriteLine(AddFileCommand.ErrorNoResponseCodes);
        }
    }
}
