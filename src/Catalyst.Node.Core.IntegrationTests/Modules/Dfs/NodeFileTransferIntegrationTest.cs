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
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Polly;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using TransferFileBytesRequestHandler = Catalyst.Node.Core.RPC.Handlers.TransferFileBytesRequestHandler;

namespace Catalyst.Node.Core.IntegrationTests.Modules.Dfs
{
    public sealed class NodeFileTransferIntegrationTest : FileSystemBasedTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _nodeFileTransferFactory;
        private readonly IDfs _dfs;
        private readonly IpfsAdapter _ipfsEngine;
        private readonly IMessageFactory _messageFactory;

        public NodeFileTransferIntegrationTest(ITestOutputHelper testOutput) : base(testOutput)
        {
            var config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build(), "NodeFileTransferIntegrationTest");

            var peerSettings = new PeerSettings(config);
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _messageFactory = Substitute.For<IMessageFactory>();
            _nodeFileTransferFactory = new DownloadFileTransferFactory();

            var passwordReader = new TestPasswordReader("abcd");

            _ipfsEngine = new IpfsAdapter(passwordReader, peerSettings, FileSystem, _logger);
            _logger = Substitute.For<ILogger>();
            _dfs = new Core.Modules.Dfs.Dfs(_ipfsEngine, _logger);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Cancel_File_Transfer()
        {
            var sender = new PeerIdentifier(PeerIdHelper.GetPeerId("sender"));

            IDownloadFileInformation fileTransferInformation = new DownloadFileTransferInformation(
                sender,
                sender,
                _fakeContext.Channel,
                Guid.NewGuid(),
                string.Empty,
                555);

            var cancellationTokenSource = new CancellationTokenSource();
            _nodeFileTransferFactory.RegisterTransfer(fileTransferInformation);
            _nodeFileTransferFactory.FileTransferAsync(fileTransferInformation.CorrelationGuid, cancellationTokenSource.Token);
            Assert.Single(_nodeFileTransferFactory.Keys);
            cancellationTokenSource.Cancel();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var linearBackOffRetryPolicy = Policy.Handle<TaskCanceledException>()
               .WaitAndRetryAsync(5, retryAttempt =>
               {
                   var timeSpan = TimeSpan.FromSeconds(retryAttempt + 5);
                   cts = new CancellationTokenSource(timeSpan);
                   return timeSpan;
               });

            linearBackOffRetryPolicy.ExecuteAsync(() =>
            {
                return Task.Run(() =>
                {
                    while (!fileTransferInformation.IsCompleted)
                    {
                        Task.Delay(1000, cts.Token).GetAwaiter().GetResult();
                    }
                }, cts.Token);
            }).GetAwaiter().GetResult();

            var fileCleanedUp = !File.Exists(fileTransferInformation.TempPath);

            Assert.True(fileTransferInformation.IsExpired());
            Assert.True(fileCleanedUp);
            Assert.Empty(_nodeFileTransferFactory.Keys);
        }

        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [InlineData(1000L)]
        [InlineData(82000L)]
        [InlineData(100000L)]
        public void Verify_File_Integrity_On_Transfer(long byteSize) { AddFileToDfs(byteSize, out _); }

        private void AddFileToDfs(long byteSize, out long crcValue)
        {
            var fakeNode = Substitute.For<INodeRpcClient>();
            var sender = PeerIdHelper.GetPeerId("sender");
            var recipient = PeerIdHelper.GetPeerId("recipient");
            var senderPeerId = new PeerIdentifier(sender);
            var recipientPeerId = new PeerIdentifier(recipient);
            var fileToTransfer = FileHelper.CreateRandomTempFile(byteSize);
            var addFileToDfsRequestHandler = new AddFileToDfsRequestHandler(_dfs, senderPeerId, _nodeFileTransferFactory,
                _messageFactory, _logger);
            var transferBytesRequestHandler =
                new TransferFileBytesRequestHandler(_nodeFileTransferFactory, senderPeerId, _logger, _messageFactory);
            var uniqueFileKey = Guid.NewGuid();
            crcValue = FileHelper.GetCrcValue(fileToTransfer);

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = fileToTransfer,
                FileSize = (ulong)byteSize
            }.ToAnySigned(sender, uniqueFileKey);
            request.SendToHandler(_fakeContext, addFileToDfsRequestHandler);

            Assert.Single(_nodeFileTransferFactory.Keys);

            var fileTransferInformation =
                _nodeFileTransferFactory.GetFileTransferInformation(uniqueFileKey);
            Assert.True(fileTransferInformation.Initialised, "File transfer not initialised");

            using (var fs = File.Open(fileToTransfer, FileMode.Open))
            {
                var fileUploadInformation = new UploadFileTransferInformation(fs, senderPeerId, recipientPeerId,
                    fakeNode.Channel, uniqueFileKey, new MessageFactory());
                for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
                {
                    fileUploadInformation.GetUploadMessageDto(i)
                       .SendToHandler(_fakeContext, transferBytesRequestHandler);
                }
            }

            Assert.True(fileTransferInformation.ChunkIndicatorsTrue());

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var linearBackOffRetryPolicy = Policy.Handle<TaskCanceledException>()
               .WaitAndRetryAsync(5, retryAttempt =>
               {
                   var timeSpan = TimeSpan.FromSeconds(retryAttempt + 5);
                   cts = new CancellationTokenSource(timeSpan);
                   return timeSpan;
               });

            linearBackOffRetryPolicy.ExecuteAsync(() =>
            {
                return Task.Run(() =>
                {
                    while (fileTransferInformation.DfsHash == null)
                    {
                        Task.Delay(1000, cts.Token).GetAwaiter().GetResult();
                    }
                }, cts.Token);
            }).GetAwaiter().GetResult();

            Assert.NotNull(fileTransferInformation.DfsHash);

            long ipfsCrcValue;
            using (var ipfsStream = _dfs.ReadAsync(fileTransferInformation.DfsHash).GetAwaiter().GetResult())
            {
                ipfsCrcValue = FileHelper.GetCrcValue(ipfsStream);
            }

            Assert.Equal(crcValue, ipfsCrcValue);
        }
    }
}
