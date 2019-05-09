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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;
using Catalyst.Common.P2P;
using System.Threading;
using System.IO;
using Catalyst.Common.Config;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Microsoft.Extensions.Configuration;
using Catalyst.Node.Core.Modules.Ipfs;
using Catalyst.Node.Core.Rpc.Messaging;
using Xunit.Abstractions;
using TransferFileBytesRequestHandler = Catalyst.Node.Core.RPC.Handlers.TransferFileBytesRequestHandler;

namespace Catalyst.Node.Core.UnitTest.FileTransfer
{
    public sealed class FileTransferNodeTests : FileSystemBasedTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IFileTransfer _nodeFileTransfer;
        private readonly IDfs _dfs;
        private readonly IMessageCorrelationCache _cache;
        private readonly IpfsAdapter _ipfsEngine;

        public FileTransferNodeTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            var config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build(), "FileTransferNodeTests");

            var peerSettings = new PeerSettings(config);
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _cache = Substitute.For<IMessageCorrelationCache>();
            _nodeFileTransfer = new Common.FileTransfer.FileTransfer();

            var passwordReader = new TestPasswordReader("abcd");

            _ipfsEngine = new IpfsAdapter(passwordReader, peerSettings, FileSystem, _logger);
            _logger = Substitute.For<ILogger>();
            _dfs = new IpfsDfs(_ipfsEngine, _logger);
        }

        [Fact]
        public void Node_Initialize_File_Transfer()
        {
            var sender = PeerIdHelper.GetPeerId("sender");

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = "Test.dat",
                FileSize = 10000
            }.ToAnySigned(sender, Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var cache = Substitute.For<IMessageCorrelationCache>();
            var handler = new AddFileToDfsRequestHandler(new IpfsDfs(_ipfsEngine, _logger), new PeerIdentifier(sender),
                _nodeFileTransfer, cache, _logger);
            handler.StartObserving(messageStream);

            Assert.Equal(1, _nodeFileTransfer.Keys.Length);
        }

        [Fact]
        public void Node_File_Transfer_Timeout()
        {
            var sender = PeerIdHelper.GetPeerId("sender");
            var expiredDelegateHit = false;

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = "Test.dat",
                FileSize = 10000
            }.ToAnySigned(sender, Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var cache = Substitute.For<IMessageCorrelationCache>();
            var handler = new AddFileToDfsRequestHandler(new IpfsDfs(_ipfsEngine, _logger), new PeerIdentifier(sender),
                _nodeFileTransfer, cache, _logger);
            handler.StartObserving(messageStream);

            Assert.Equal(1, _nodeFileTransfer.Keys.Length);

            var uniqueFileKey = _nodeFileTransfer.Keys.ToList().Single();

            var fileTransferInformation = _nodeFileTransfer.GetFileTransferInformation(uniqueFileKey);
            _nodeFileTransfer.GetFileTransferInformation(uniqueFileKey)
               .AddExpiredCallback(delegate { expiredDelegateHit = true; });
            fileTransferInformation.Expire();
            Thread.Sleep(1000);

            var fileCleanedUp = !File.Exists(Path.GetTempPath() + uniqueFileKey);

            Assert.Equal(true, fileTransferInformation.IsExpired());
            Assert.Equal(true, fileCleanedUp);
            Assert.Equal(true, expiredDelegateHit);
            Assert.Equal(0, _nodeFileTransfer.Keys.Length);
        }

        [Theory]
        [InlineData(1000L)]
        [InlineData(82000L)]
        [InlineData(100000L)]
        public void Verify_File_Integrity_On_Transfer(long byteSize) { AddFileToDfs(byteSize, out _); }

        private string AddFileToDfs(long byteSize, out long crcValue)
        {
            var fakeNode = Substitute.For<INodeRpcClient>();
            var sender = PeerIdHelper.GetPeerId("sender");
            var recipient = PeerIdHelper.GetPeerId("recipient");
            var senderPeerId = new PeerIdentifier(sender);
            var recipientPeerId = new PeerIdentifier(recipient);
            var fileToTransfer = FileHelper.CreateRandomTempFile(byteSize);
            crcValue = FileHelper.GetCrcValue(fileToTransfer);

            var uniqueFileKey = Guid.NewGuid();
            string dfsHash = null;

            long storedCrc32Value = -1;

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = fileToTransfer,
                FileSize = (ulong) byteSize
            }.ToAnySigned(sender, uniqueFileKey);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            new AddFileToDfsRequestHandler(_dfs, senderPeerId, _nodeFileTransfer,
                _cache, _logger).StartObserving(messageStream);

            Assert.Equal(1, _nodeFileTransfer.Keys.Length);

            var fileTransferInformation =
                _nodeFileTransfer.GetFileTransferInformation(uniqueFileKey);

            fileTransferInformation.AddSuccessCallback(information =>
            {
                storedCrc32Value = FileHelper.GetCrcValue(information.TempPath);
                dfsHash = information.DfsHash;
            });

            var chunkMessages = new List<AnySigned>();
            using (var fs = File.Open(fileToTransfer, FileMode.Open))
            {
                var fileUploadInformation = FileTransferInformation.BuildUpload(fs, senderPeerId, recipientPeerId,
                    fakeNode.Channel, uniqueFileKey, new RpcMessageFactory<TransferFileBytesRequest>());
                for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
                {
                    var transferMessage = fileUploadInformation
                       .GetUploadMessage(i)
                       .ToAnySigned(recipient, uniqueFileKey);
                    chunkMessages.Add(transferMessage);
                }
            }

            messageStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, chunkMessages.ToArray());
            new TransferFileBytesRequestHandler(_nodeFileTransfer, senderPeerId, _cache, _logger).StartObserving(messageStream);

            Assert.NotNull(dfsHash);
            Assert.Equal(crcValue, storedCrc32Value);
            File.Delete(fileToTransfer);
            Assert.True(fileTransferInformation.IsComplete());

            long ipfsCrcValue = -1;
            using (var ipfsStream = _dfs.ReadAsync(dfsHash).GetAwaiter().GetResult())
            {
                ipfsCrcValue = FileHelper.GetCrcValue(ipfsStream);
            }

            Assert.Equal(crcValue, ipfsCrcValue);
            return dfsHash;
        }

        #region IDisposable Support

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ipfsEngine?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
