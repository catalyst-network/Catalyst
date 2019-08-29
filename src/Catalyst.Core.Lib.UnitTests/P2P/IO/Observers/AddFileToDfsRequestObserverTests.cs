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
using System.IO;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Types;
using Catalyst.Core.Lib.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Observers
{
    public sealed class AddFileToDfsRequestObserverTests
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _nodeFileTransferFactory;
        private readonly AddFileToDfsRequestObserver _addFileToDfsRequestObserver;
        private readonly IPeerIdentifier _senderIdentifier;
        private readonly IDfs _fakeDfs;

        public AddFileToDfsRequestObserverTests()
        {
            _senderIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");
            _fakeDfs = Substitute.For<IDfs>();
            var logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _nodeFileTransferFactory = Substitute.For<IDownloadFileTransferFactory>();
            _addFileToDfsRequestObserver = new AddFileToDfsRequestObserver(_fakeDfs,
                _senderIdentifier,
                _nodeFileTransferFactory,
                logger);
        }

        [Fact]
        public void Handler_Can_Initialize_Download_File_Transfer()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            var correlationId = CorrelationId.GenerateCorrelationId();

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = "Test.dat",
                FileSize = 10000
            }.ToProtocolMessage(_senderIdentifier.PeerId, correlationId);
            request.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);

            _nodeFileTransferFactory.RegisterTransfer(
                Arg.Is<IDownloadFileInformation>(
                    info => info.CorrelationId.Id.Equals(correlationId.Id)));
            AssertResponse(FileTransferResponseCodeTypes.Successful);
        }

        [Fact]
        public void Handler_Sends_Error_On_Invalid_Message()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>()).Throws(new Exception());
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = string.Empty,
                FileSize = 1
            }.ToProtocolMessage(_senderIdentifier.PeerId, CorrelationId.GenerateCorrelationId());

            request.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);
            AssertResponse(FileTransferResponseCodeTypes.Error);
        }

        [Fact]
        public async Task Successful_Add_File_Can_Respond_With_Finished_Code()
        {
            await Setup_File_Transfer_Response_Test(FileTransferResponseCodeTypes.Finished).ConfigureAwait(false);
        }

        [Fact]
        public async Task Dfs_Failure_Can_Respond_With_Failed_Code()
        {
            await Setup_File_Transfer_Response_Test(FileTransferResponseCodeTypes.Failed).ConfigureAwait(false);
        }

        private async Task Setup_File_Transfer_Response_Test(FileTransferResponseCodeTypes expectedResponse)
        {
            IDownloadFileInformation fileTransferInformation = null;
            var expectedHash = string.Empty;

            if (expectedResponse == FileTransferResponseCodeTypes.Finished)
            {
                expectedHash = "expectedHash";
                _fakeDfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>()).Returns(expectedHash);
            }
            else
            {
                _fakeDfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>()).Throws(new Exception());
            }

            _nodeFileTransferFactory.RegisterTransfer(Arg.Do<IDownloadFileInformation>(information =>
            {
                fileTransferInformation = information;
                fileTransferInformation.RecipientChannel = Substitute.For<IChannel>();
                fileTransferInformation.UpdateChunkIndicator(0, true);
                fileTransferInformation.Dispose();
            }));

            Handler_Can_Initialize_Download_File_Transfer();

            var success = await TaskHelper.WaitForAsync(() =>
            {
                try
                {
                    fileTransferInformation.RecipientChannel.Received(1).WriteAndFlushAsync(
                        Arg.Any<DefaultAddressedEnvelope<ProtocolMessage>>());
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(5));

            success.Should().BeTrue();
        }

        private void AssertResponse(FileTransferResponseCodeTypes sentResponse)
        {
            _fakeContext.Channel.Received(1).WriteAndFlushAsync(
                Arg.Is<DefaultAddressedEnvelope<ProtocolMessage>>(
                    t => t.Content.FromProtocolMessage<AddFileToDfsResponse>().ResponseCode[0] == sentResponse.Id));
        }
    }
}
