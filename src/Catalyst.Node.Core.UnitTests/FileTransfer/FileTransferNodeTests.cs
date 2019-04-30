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
using Catalyst.Node.Core.Modules.FileTransfer;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;
using Catalyst.Common.P2P;
using System.Threading;
using Catalyst.Common.FileTransfer;
using System.IO;
using Catalyst.Cli.FileTransfer;
using Catalyst.Common.Config;
using Catalyst.Common.FileSystem;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using ICSharpCode.SharpZipLib.Checksum;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core.UnitTest.FileTransfer
{
    public sealed class FileTransferNodeTests : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IFileTransfer _fileTransfer;
        private readonly IIpfsEngine _ipfsEngine;
        private AddFileToDfsRequestHandler _handler;

        public FileTransferNodeTests()
        {
            var config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build(), "FileTransferNodeTests");

            var peerSettings = new PeerSettings(config);
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fileTransfer = new Core.Modules.FileTransfer.FileTransfer();

            var passwordReader = Substitute.For<IPasswordReader>();
            passwordReader.ReadSecurePassword().ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("abcd"));
            _ipfsEngine = new IpfsEngine(passwordReader, peerSettings, new FileSystem(), _logger);

            _logger = Substitute.For<ILogger>();
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
                FileSize = (ulong) 10000
            }.ToAnySigned(sender, Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var cache = Substitute.For<IMessageCorrelationCache>();
            _handler = new AddFileToDfsRequestHandler(new IpfsDfs(_ipfsEngine, _logger), new PeerIdentifier(sender), _fileTransfer, cache, _logger);
            _handler.StartObserving(messageStream);

            Assert.Equal(1, _fileTransfer.Keys.Length);
        }

        [Fact]
        public void Node_File_Transfer_Timeout()
        {
            var sender = PeerIdHelper.GetPeerId("sender");
            var expiredDelegateHit = false;

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest()
            {
                Node = "node1",
                FileName = "Test.dat",
                FileSize = (ulong) 10000
            }.ToAnySigned(sender, Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var cache = Substitute.For<IMessageCorrelationCache>();
            _handler = new AddFileToDfsRequestHandler(new IpfsDfs(_ipfsEngine, _logger), new PeerIdentifier(sender), _fileTransfer, cache, _logger);
            _handler.StartObserving(messageStream);

            Assert.Equal(1, _fileTransfer.Keys.Length);

            var uniqueFileKey = _fileTransfer.Keys.ToList().Single();

            var fileTransferInformation = _fileTransfer.GetFileTransferInformation(uniqueFileKey);
            _fileTransfer.GetFileTransferInformation(uniqueFileKey).OnExpired += delegate { expiredDelegateHit = true; };
            Thread.Sleep((FileTransferConstants.ExpiryMinutes * 60 * 1000) + 1000);
            bool fileCleanedUp = !File.Exists(Path.GetTempPath() + uniqueFileKey);

            Assert.Equal(true, fileTransferInformation.IsExpired());
            Assert.Equal(true, fileCleanedUp);
            Assert.Equal(true, expiredDelegateHit);
            Assert.Equal(0, _fileTransfer.Keys.Length);
        }

        [Theory]
        [InlineData(10000100L)]
        [InlineData(82000L)]
        [InlineData(100000L)]
        public void Verify_File_Integrity_On_Transfer(long byteSize)
        {
            var fileToTransfer = Path.GetTempPath() + Guid.NewGuid().ToString();
            INodeRpcClient fakeNode = Substitute.For<INodeRpcClient>();
            if (File.Exists(fileToTransfer))
            {
                File.Delete(fileToTransfer);
            }

            Crc32 crc32 = new Crc32();

            var sender = PeerIdHelper.GetPeerId("sender");
            var reciepient = PeerIdHelper.GetPeerId("reciepient");

            var senderPeerId = new PeerIdentifier(sender);

            var uniqueFileKey = Guid.NewGuid();

            byte[] b = new byte[byteSize];
            new Random().NextBytes(b);
            FileStream fs = File.Create(fileToTransfer);
            fs.Write(b);
            fs.Close();
            crc32.Update(b);

            var crc32OriginalValue = crc32.Value;
            long storedCrc32Value = -1;

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = fileToTransfer,
                FileSize = (ulong) b.Length
            }.ToAnySigned(sender, uniqueFileKey);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            fakeNode.MessageStream.Returns(messageStream);
            var cache = Substitute.For<IMessageCorrelationCache>();
            _handler = new AddFileToDfsRequestHandler(new IpfsDfs(_ipfsEngine, _logger), senderPeerId, _fileTransfer, cache, _logger);
            _handler.StartObserving(messageStream);

            Assert.Equal(1, _fileTransfer.Keys.Length);

            FileTransferInformation fileTransferInformation =
                _fileTransfer.GetFileTransferInformation(uniqueFileKey.ToString());
            fileTransferInformation.OnSuccess += information =>
            {
                crc32.Reset();
                crc32.Update(File.ReadAllBytes(information.TempPath));
                storedCrc32Value = crc32.Value;
            };

            fs = File.Open(fileToTransfer, FileMode.Open);

            List<AnySigned> chunkMessages = new List<AnySigned>();
            for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
            {
                var transferMessage = CliFileTransfer.Instance.GetFileTransferRequestMessage(fs, uniqueFileKey.ToByteString(), b.Length, i).ToAnySigned(reciepient, Guid.NewGuid());
                chunkMessages.Add(transferMessage);
            }

            fs.Close();

            messageStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, chunkMessages.ToArray());
            var transferFileBytesHandler = new TransferFileBytesRequestHandler(_fileTransfer, senderPeerId, cache, _logger);
            transferFileBytesHandler.StartObserving(messageStream);

            Assert.NotNull(fileTransferInformation.DfsHash);
            Assert.Equal(crc32OriginalValue, storedCrc32Value);
            File.Delete(fileToTransfer);
        }

        public void Dispose()
        {
            _handler.Dispose();
        }
    }
}
