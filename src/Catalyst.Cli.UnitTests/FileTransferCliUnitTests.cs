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
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using Catalyst.Cli.Handlers;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.Modules.FileTransfer;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;
using Ipfs.CoreApi;
using Ipfs;
using Catalyst.Common.P2P;

namespace Catalyst.Cli.UnitTests
{
    public sealed class FileTransferCliUnitTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IUserOutput _output;
        private readonly IFileTransfer _fileTransfer;
        private readonly IIpfsEngine _ipfsEngine;
        private readonly Cid _cid;
        private readonly IFileSystemNode _fileSystemNode;
        
        public static List<object[]> QueryContents;

        private AddFileToDfsRequestHandler _handler;
        
        public FileTransferCliUnitTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
            _fileTransfer = new Node.Core.Modules.FileTransfer.FileTransfer();

            _ipfsEngine = Substitute.For<IIpfsEngine>();
            var fileSystem = Substitute.For<IFileSystemApi>();
            _ipfsEngine.FileSystem.Returns(fileSystem);

            _logger = Substitute.For<ILogger>();
            var hashBits = Guid.NewGuid().ToByteArray().Concat(new byte[16]).ToArray();
            _cid = new Cid
            {
                Encoding = "base64",
                Hash = new MultiHash(IpfsDfs.HashAlgorithm, hashBits)
            };

            _fileSystemNode = Substitute.For<IFileSystemNode>();
            _fileSystemNode.Id.ReturnsForAnyArgs(_cid);
        }

        [Theory]
        [InlineData(10000L)]
        public void RpcClient_Can_Initialize_Transfer(long fileBufferLength)
        {
            byte[] fileBytes = new byte[fileBufferLength];

            var sender = PeerIdHelper.GetPeerId("sender");
            //Create a response object and set its return value
            var request = new AddFileToDfsRequest()
            {
                Node = "node1",
                FileName = "Test.dat",
                FileSize = (ulong) fileBufferLength
            }.ToAnySigned(sender, Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var cache = Substitute.For<IMessageCorrelationCache>();
            _handler = new AddFileToDfsRequestHandler(new IpfsDfs(_ipfsEngine, _logger), new PeerIdentifier(sender), _fileTransfer, cache, _logger);
            _handler.StartObserving(messageStream);
        }


        public void Dispose()
        {
            _handler.Dispose();
        }
    }
}
