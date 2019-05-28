using System;
using System.Threading;
using Catalyst.Cli.Handlers;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    public class AddFileToDfsResponseHandlerTests
    {
        private readonly IUserOutput _userOutput;
        private readonly IUploadFileTransferFactory _uploadFileTransferFactory;
        private readonly AddFileToDfsResponseHandler _addFileToDfsResponseHandler;
        private readonly IChannelHandlerContext _channelHandlerContext;

        public AddFileToDfsResponseHandlerTests()
        {
            _userOutput = Substitute.For<IUserOutput>();
            _uploadFileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
            _channelHandlerContext = Substitute.For<IChannelHandlerContext>();

            _addFileToDfsResponseHandler = new AddFileToDfsResponseHandler(
                Substitute.For<IRpcCorrelationCache>(),
                Substitute.For<ILogger>(),
                _uploadFileTransferFactory,
                _userOutput
            );
        }

        [Fact]
        public void AddFileToDfsResponseHandlerPrintsMessageOnFailureOrSuccess()
        {
            _addFileToDfsResponseHandler.HandleMessage(GetAddFileToDfsResponse(FileTransferResponseCodes.Finished));
            _addFileToDfsResponseHandler.HandleMessage(GetAddFileToDfsResponse(FileTransferResponseCodes.Failed));
            _userOutput.Received(Quantity.Exactly(2)).WriteLine(Arg.Any<string>());
        }

        [Fact]
        public void InitializesFileTransferOnSuccessResponse()
        {
            _addFileToDfsResponseHandler.HandleMessage(GetAddFileToDfsResponse(FileTransferResponseCodes.Successful));
            _uploadFileTransferFactory.Received(Quantity.Exactly(1))
               .FileTransferAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        private IChanneledMessage<AnySigned> GetAddFileToDfsResponse(FileTransferResponseCodes responseCode)
        {
            AddFileToDfsResponse addFileResponse = new AddFileToDfsResponse
            {
                DfsHash = "Test",
                ResponseCode = ByteString.CopyFrom((byte) responseCode)
            };

            AnySigned anySigned = addFileResponse.ToAnySigned(PeerIdHelper.GetPeerId("Test"), Guid.NewGuid());
            return new ChanneledAnySigned(_channelHandlerContext, anySigned);
        }
    }
}
