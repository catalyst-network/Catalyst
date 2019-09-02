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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.Rpc;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.Dfs;
using Catalyst.Core.Extensions;
using Catalyst.Core.FileTransfer;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.Dfs
{
    public sealed class NodeFileTransferIntegrationTests : FileSystemBasedTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _nodeFileTransferFactory;
        private readonly IDfs _dfs;

        public NodeFileTransferIntegrationTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            var configurationRoot = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet)))
               .Build();

            SocketPortHelper.AlterConfigurationToGetUniquePort(configurationRoot, "NodeFileTransferIntegrationTest");

            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _nodeFileTransferFactory = new DownloadFileTransferFactory(_logger);

            var passwordManager = Substitute.For<IPasswordManager>();
            passwordManager
               .RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.IpfsPassword, Arg.Any<string>())
               .Returns(TestPasswordReader.BuildSecureStringPassword("abcd"));

            var ipfsEngine = new IpfsAdapter(passwordManager, FileSystem, _logger);

            _logger = Substitute.For<ILogger>();
            _dfs = new Core.Dfs.Dfs(ipfsEngine, _logger);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Cancel_File_Transfer()
        {
            var sender = new PeerIdentifier(PeerIdHelper.GetPeerId("sender"));

            IDownloadFileInformation fileTransferInformation = new DownloadFileTransferInformation(
                sender,
                sender,
                _fakeContext.Channel,
                CorrelationId.GenerateCorrelationId(),
                string.Empty,
                555);

            var cancellationTokenSource = new CancellationTokenSource();
            _nodeFileTransferFactory.RegisterTransfer(fileTransferInformation);
            _nodeFileTransferFactory
               .FileTransferAsync(fileTransferInformation.CorrelationId, cancellationTokenSource.Token)
               .ConfigureAwait(false).GetAwaiter();
            Assert.Single(_nodeFileTransferFactory.Keys);
            cancellationTokenSource.Cancel();

            await TaskHelper.WaitForAsync(() => fileTransferInformation.IsCompleted, TimeSpan.FromSeconds(10));

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
        public async Task Verify_File_Integrity_On_Transfer(long byteSize)
        {
            await AddFileToDfs(byteSize).ConfigureAwait(false);
        }

        private async Task AddFileToDfs(long byteSize)
        {
            var fakeNode = Substitute.For<INodeRpcClient>();
            var sender = PeerIdHelper.GetPeerId("sender");
            var recipient = PeerIdHelper.GetPeerId("recipient");
            var senderPeerId = new PeerIdentifier(sender);
            var recipientPeerId = new PeerIdentifier(recipient);
            var fileToTransfer = FileHelper.CreateRandomTempFile(byteSize);
            var addFileToDfsRequestHandler =
                new AddFileToDfsRequestObserver(_dfs, senderPeerId, _nodeFileTransferFactory, _logger);
            var transferBytesRequestHandler =
                new TransferFileBytesRequestObserver(_nodeFileTransferFactory, senderPeerId, _logger);

            var uniqueFileKey = CorrelationId.GenerateCorrelationId();
            var crcValue = FileHelper.GetCrcValue(fileToTransfer);

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = fileToTransfer,
                FileSize = (ulong) byteSize
            }.ToProtocolMessage(sender, uniqueFileKey);
            request.SendToHandler(_fakeContext, addFileToDfsRequestHandler);

            Assert.Single(_nodeFileTransferFactory.Keys);

            var fileTransferInformation =
                _nodeFileTransferFactory.GetFileTransferInformation(uniqueFileKey);
            Assert.True(fileTransferInformation.Initialised, "File transfer not initialised");

            using (var fs = File.Open(fileToTransfer, FileMode.Open))
            {
                var fileUploadInformation = new UploadFileTransferInformation(fs, senderPeerId, recipientPeerId,
                    fakeNode.Channel, uniqueFileKey);
                for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
                {
                    fileUploadInformation.GetUploadMessageDto(i).Content
                       .SendToHandler(_fakeContext, transferBytesRequestHandler);
                }
            }

            Assert.True(fileTransferInformation.ChunkIndicatorsTrue());

            await TaskHelper.WaitForAsync(() => fileTransferInformation.DfsHash != null, TimeSpan.FromSeconds(15));
            Assert.NotNull(fileTransferInformation.DfsHash);

            long ipfsCrcValue;
            using (var ipfsStream = await _dfs.ReadAsync(fileTransferInformation.DfsHash))
            {
                ipfsCrcValue = FileHelper.GetCrcValue(ipfsStream);
            }

            Assert.Equal(crcValue, ipfsCrcValue);
        }
    }
}
