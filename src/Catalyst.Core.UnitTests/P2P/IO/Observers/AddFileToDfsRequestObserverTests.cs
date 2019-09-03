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
using System.Threading;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.Rpc.IO.Observers;
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

namespace Catalyst.Core.UnitTests.P2P.IO.Observers
{
    public sealed class AddFileToDfsRequestObserverTests
    {
        private readonly ManualResetEvent _manualResetEvent;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _nodeFileTransferFactory;
        private readonly AddFileToDfsRequestObserver _addFileToDfsRequestObserver;
        private readonly IPeerIdentifier _senderIdentifier;
        private readonly IDfs _fakeDfs;

        public AddFileToDfsRequestObserverTests()
        {
            _manualResetEvent = new ManualResetEvent(false);
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
        public void Handler_Uses_Correct_CorrelationId()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            var correlationId = CorrelationId.GenerateCorrelationId();

            var protocolMessage = GenerateProtocolMessage(correlationId);

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);

            _nodeFileTransferFactory.RegisterTransfer(
                Arg.Is<IDownloadFileInformation>(
                    info => info.CorrelationId.Id.Equals(correlationId.Id)));
        }

        [Fact]
        public void Handler_Can_Initialize_Download_File_Transfer()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            var protocolMessage = GenerateProtocolMessage();

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);

            _fakeContext.Channel.Received(1).WriteAndFlushAsync(
                Arg.Is<DefaultAddressedEnvelope<ProtocolMessage>>(
                    t => t.Content.FromProtocolMessage<AddFileToDfsResponse>().ResponseCode[0] == FileTransferResponseCodeTypes.Successful.Id));
        }

        [Fact]
        public void Handler_Sends_Error_On_Invalid_Message()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>()).Throws(new Exception());

            var protocolMessage = GenerateProtocolMessage();

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);

            _fakeContext.Channel.Received(1).WriteAndFlushAsync(
                Arg.Is<DefaultAddressedEnvelope<ProtocolMessage>>(
                    t => t.Content.FromProtocolMessage<AddFileToDfsResponse>().ResponseCode[0] == FileTransferResponseCodeTypes.Error.Id));
        }

        [Fact]
        public void Successful_Add_File_Can_Respond_With_Finished_Code()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            var expectedHash = "expectedHash";
            _fakeDfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>()).Returns(expectedHash);

            var protocolMessage = GenerateProtocolMessage();

            AddFileToDfsResponse addFileToDfsResponse = null;
            _nodeFileTransferFactory.RegisterTransfer(Arg.Do<IDownloadFileInformation>(async information =>
            {
                information.RecipientChannel = Substitute.For<IChannel>();
                information.UpdateChunkIndicator(0, true);
                information.Dispose();
                await information.RecipientChannel.WriteAndFlushAsync(Arg.Do<MessageDto>(x =>
                {
                    addFileToDfsResponse = x.Content.FromProtocolMessage<AddFileToDfsResponse>();
                    _manualResetEvent.Set();
                }));
            }));

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);
            _manualResetEvent.WaitOne();

            addFileToDfsResponse.ResponseCode[0].Should().Be((byte) FileTransferResponseCodeTypes.Finished.Id);
            addFileToDfsResponse.DfsHash.Should().Be(expectedHash);
        }

        [Fact]
        public void Dfs_Failure_Can_Respond_With_Failed_Code()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            _fakeDfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>()).Throws(new Exception());

            var protocolMessage = GenerateProtocolMessage();

            AddFileToDfsResponse addFileToDfsResponse = null;
            _nodeFileTransferFactory.RegisterTransfer(Arg.Do<IDownloadFileInformation>(async information =>
            {
                information.RecipientChannel = Substitute.For<IChannel>();
                information.UpdateChunkIndicator(0, true);
                information.Dispose();
                await information.RecipientChannel.WriteAndFlushAsync(Arg.Do<MessageDto>(x =>
                {
                    addFileToDfsResponse = x.Content.FromProtocolMessage<AddFileToDfsResponse>();
                    _manualResetEvent.Set();
                }));
            }));

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);
            _manualResetEvent.WaitOne();

            addFileToDfsResponse.ResponseCode[0].Should().Be((byte) FileTransferResponseCodeTypes.Failed.Id);
        }

        private ProtocolMessage GenerateProtocolMessage(ICorrelationId correlationId = null)
        {
            return new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = "Test.dat",
                FileSize = 10000
            }.ToProtocolMessage(_senderIdentifier.PeerId, correlationId);
        }
    }
}
