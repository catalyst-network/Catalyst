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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Options;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using MultiFormats.Registry;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using NUnit.Framework;

//@TODO should be in rpc module test

namespace Catalyst.Core.Modules.Rpc.Server.Tests.UnitTests
{
    public sealed class AddFileToDfsRequestObserverTests
    {
        private ManualResetEvent _manualResetEvent;
        private IChannelHandlerContext _fakeContext;
        private IDownloadFileTransferFactory _nodeFileTransferFactory;
        private AddFileToDfsRequestObserver _addFileToDfsRequestObserver;
        private PeerId _senderIdentifier;
        private IDfsService _fakeDfsService;
        private IHashProvider _hashProvider;

        [SetUp]
        public void Init()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _manualResetEvent = new ManualResetEvent(false);
            _senderIdentifier = PeerIdHelper.GetPeerId("sender");
            var peerSettings = _senderIdentifier.ToSubstitutedPeerSettings();
            _fakeDfsService = Substitute.For<IDfsService>();
            var logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _nodeFileTransferFactory = Substitute.For<IDownloadFileTransferFactory>();
            _addFileToDfsRequestObserver = new AddFileToDfsRequestObserver(_fakeDfsService,
                peerSettings,
                _nodeFileTransferFactory,
                _hashProvider,
                logger);
        }

        [TearDown]
        public void TearDown()
        {
            _manualResetEvent.Dispose();
            _fakeDfsService.Dispose();
            _addFileToDfsRequestObserver.Dispose();
        }

        [Test]
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

        [Test]
        public void Handler_Can_Initialize_Download_File_Transfer()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            var protocolMessage = GenerateProtocolMessage();

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);

            _fakeContext.Channel.Received(1)?.WriteAndFlushAsync(
                Arg.Is<DefaultAddressedEnvelope<ProtocolMessage>>(
                    t => t.Content.FromProtocolMessage<AddFileToDfsResponse>().ResponseCode[0] == FileTransferResponseCodeTypes.Successful.Id));
        }

        [Test]
        public void Handler_Sends_Error_On_Invalid_Message()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>()).Throws(new Exception());

            var protocolMessage = GenerateProtocolMessage();

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);

            _fakeContext.Channel.Received(1)?.WriteAndFlushAsync(
                Arg.Is<DefaultAddressedEnvelope<ProtocolMessage>>(
                    t => t.Content.FromProtocolMessage<AddFileToDfsResponse>().ResponseCode[0] == FileTransferResponseCodeTypes.Error.Id));
        }

        [Test]
        public void Successful_Add_File_Can_Respond_With_Finished_Code()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            var expectedCid = _hashProvider.ComputeUtf8MultiHash("expectedHash").ToCid();
            var fakeBlock = Substitute.For<IFileSystemNode>();
            fakeBlock.Id.Returns(expectedCid);
            _fakeDfsService.UnixFsApi.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<AddFileOptions>()).Returns(fakeBlock);

            var protocolMessage = GenerateProtocolMessage();

            AddFileToDfsResponse addFileToDfsResponse = null;

            async void UseArgument(IDownloadFileInformation information)
            {
                information.RecipientChannel = Substitute.For<IChannel>();
                information.UpdateChunkIndicator(0, true);
                information.Dispose();
                await information.RecipientChannel.WriteAndFlushAsync(Arg.Do<MessageDto>(x =>
                {
                    addFileToDfsResponse = x.Content.FromProtocolMessage<AddFileToDfsResponse>();
                    _manualResetEvent.Set();
                }));
            }

            _nodeFileTransferFactory.RegisterTransfer(Arg.Do<IDownloadFileInformation>(UseArgument));

            protocolMessage.SendToHandler(_fakeContext, _addFileToDfsRequestObserver);
            _manualResetEvent.WaitOne();

            addFileToDfsResponse.ResponseCode[0].Should().Be((byte) FileTransferResponseCodeTypes.Finished.Id);
            addFileToDfsResponse.DfsHash.Should().Be(expectedCid);
        }

        [Test]
        public void Dfs_Failure_Can_Respond_With_Failed_Code()
        {
            _nodeFileTransferFactory.RegisterTransfer(Arg.Any<IDownloadFileInformation>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            _fakeDfsService.UnixFsApi.AddAsync(Arg.Any<Stream>(), Arg.Any<string>()).Throws(new Exception());

            var protocolMessage = GenerateProtocolMessage();

            AddFileToDfsResponse addFileToDfsResponse = null;

            async void UseArgument(IDownloadFileInformation information)
            {
                information.RecipientChannel = Substitute.For<IChannel>();
                information.UpdateChunkIndicator(0, true);
                information.Dispose();
                await information.RecipientChannel.WriteAndFlushAsync(Arg.Do<MessageDto>(x =>
                {
                    addFileToDfsResponse = x.Content.FromProtocolMessage<AddFileToDfsResponse>();
                    _manualResetEvent.Set();
                }));
            }

            _nodeFileTransferFactory.RegisterTransfer(Arg.Do<IDownloadFileInformation>(UseArgument));

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
            }.ToProtocolMessage(_senderIdentifier, correlationId);
        }
    }
}
