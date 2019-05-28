using System;
using System.IO;
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.Messaging.Handlers
{
    public class GetFileFromDfsRequestHandlerTests
    {
        private readonly IRpcMessageFactory _rpcMessageFactory;
        private readonly IUploadFileTransferFactory _fileTransferFactory;
        private readonly IDfs _dfs;
        private readonly GetFileFromDfsRequestHandler _handler;

        public GetFileFromDfsRequestHandlerTests()
        {
            _rpcMessageFactory = Substitute.For<IRpcMessageFactory>();
            _fileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
            _dfs = Substitute.For<IDfs>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("test");
            _handler = new GetFileFromDfsRequestHandler(_dfs, peerIdentifier, _fileTransferFactory, _rpcMessageFactory, Substitute.For<ILogger>());
        }

        [Fact]
        public void GetFileRequestHandlerInitializesFileUpload()
        {
            using (GetFakeDfsStream(FileTransferResponseCodes.Successful))
            {
                _handler.HandleMessage(GetFileFromDfsRequestMessage());
                _dfs.Received(1).ReadAsync(Arg.Any<string>());
                _fileTransferFactory.Received(1).FileTransferAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public void FileTransferStreamIsDisposedOnError()
        {
            using (var fakeStream = GetFakeDfsStream(FileTransferResponseCodes.Error))
            {
                var message = GetFileFromDfsRequestMessage();
                _handler.HandleMessage(message);
                Assert.False(fakeStream.CanRead);
            }
        }

        private MemoryStream GetFakeDfsStream(FileTransferResponseCodes fakeResponse)
        {
            var fakeStream = new MemoryStream();
            fakeStream.Write(new byte[50]);
            _dfs.ReadAsync(Arg.Any<string>()).Returns(fakeStream);
            _fileTransferFactory.RegisterTransfer(Arg.Any<IUploadFileInformation>()).Returns(fakeResponse);
            return fakeStream;
        }

        private IChanneledMessage<AnySigned> GetFileFromDfsRequestMessage()
        {
            var getFileFromDfsRequestMessage = new GetFileFromDfsRequest
            {
                DfsHash = "test"
            };
            var anySigned = getFileFromDfsRequestMessage
               .ToAnySigned(PeerIdHelper.GetPeerId("TestMan"), Guid.NewGuid());
            return new ChanneledAnySigned(Substitute.For<IChannelHandlerContext>(), anySigned);
        }
    }
}
