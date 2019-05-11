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
using Catalyst.Cli.Rpc;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Modules.Dfs;
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
        private readonly IpfsAdapter _ipfsEngine;
        private readonly IFileTransfer _fileTransfer;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IMessageCorrelationCache _subbedCorrelationCache;

        public FileTransferNodeTests()
        {
            _subbedCorrelationCache = Substitute.For<IMessageCorrelationCache>();
            var config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build(), "FileTransferNodeTests");

            var peerSettings = new PeerSettings(config);
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fileTransfer = new Common.FileTransfer.FileTransfer();

            var passwordReader = Substitute.For<IPasswordReader>();
            passwordReader.ReadSecurePassword().ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("abcd"));

            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetCatalystHomeDir().Returns(new DirectoryInfo(Path.GetTempPath()));

            _ipfsEngine = new IpfsAdapter(passwordReader, peerSettings, fileSystem, _logger);

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
                FileSize = 10000
            }.ToAnySigned(sender, Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var cache = Substitute.For<IMessageCorrelationCache>();
            var handler = new AddFileToDfsRequestHandler(new Dfs(_ipfsEngine, _logger), new PeerIdentifier(sender),
                _fileTransfer, cache, _logger);
            handler.StartObservingMessageStreams(messageStream);

            Assert.Equal(1, _fileTransfer.Keys.Length);
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
            var handler = new AddFileToDfsRequestHandler(new Dfs(_ipfsEngine, _logger), new PeerIdentifier(sender),
                _fileTransfer, cache, _logger);
            handler.StartObservingMessageStreams(messageStream);

            Assert.Equal(1, _fileTransfer.Keys.Length);

            var uniqueFileKey = _fileTransfer.Keys.ToList().Single();

            var fileTransferInformation = _fileTransfer.GetFileTransferInformation(uniqueFileKey);
            _fileTransfer.GetFileTransferInformation(uniqueFileKey)
               .AddExpiredCallback(delegate { expiredDelegateHit = true; });

            Thread.Sleep(Constants.FileTransferExpiryMinutes * 60 * 1000 + 1000);

            var fileCleanedUp = !File.Exists(Path.GetTempPath() + uniqueFileKey);

            Assert.Equal(true, fileTransferInformation.IsExpired());
            Assert.Equal(true, fileCleanedUp);
            Assert.Equal(true, expiredDelegateHit);
            Assert.Equal(0, _fileTransfer.Keys.Length);
        }

        [Theory]
        [InlineData(1000L)]
        [InlineData(82000L)]
        [InlineData(100000L)]
        public void Verify_File_Integrity_On_Transfer(long byteSize)
        {
            var ipfsPath = Path.GetTempPath() + "ipfs";

            if (Directory.Exists(ipfsPath))
            {
                Directory.Delete(ipfsPath, true);
            }

            var fileToTransfer = Path.GetTempPath() + Guid.NewGuid();
            var fakeNode = Substitute.For<INodeRpcClient>();
            var crc32 = new Crc32();
            var sender = PeerIdHelper.GetPeerId("sender");
            var recipient = PeerIdHelper.GetPeerId("recipient");
            var senderPeerId = new PeerIdentifier(sender);
            var cliFileTransfer = new RpcFileTransfer(_subbedCorrelationCache);
            var uniqueFileKey = Guid.NewGuid();
            string dfsHash = null;

            var b = new byte[byteSize];
            new Random().NextBytes(b);
            var fs = File.Create(fileToTransfer);
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
            var dfs = new Dfs(_ipfsEngine, _logger);

            var handler = new AddFileToDfsRequestHandler(dfs, senderPeerId, _fileTransfer,
                cache, _logger);
            handler.StartObservingMessageStreams(messageStream);

            Assert.Equal(1, _fileTransfer.Keys.Length);

            var fileTransferInformation =
                _fileTransfer.GetFileTransferInformation(uniqueFileKey.ToString());

            fileTransferInformation.AddSuccessCallback(information =>
            {
                crc32.Reset();
                crc32.Update(File.ReadAllBytes(information.TempPath));
                storedCrc32Value = crc32.Value;
                dfsHash = information.DfsHash;
            });

            var chunkMessages = new List<AnySigned>();
            using (fs = File.Open(fileToTransfer, FileMode.Open))
            {
                for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
                {
                    var transferMessage = cliFileTransfer
                       .GetFileTransferRequestMessage(fs, uniqueFileKey.ToByteString(), b.Length, i)
                       .ToAnySigned(recipient, Guid.NewGuid());
                    chunkMessages.Add(transferMessage);
                }

                fs.Close();
            }

            messageStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, chunkMessages.ToArray());
            var transferFileBytesHandler =
                new TransferFileBytesRequestHandler(_fileTransfer, senderPeerId, cache, _logger);
            transferFileBytesHandler.StartObservingMessageStreams(messageStream);

            Assert.NotNull(dfsHash);
            Assert.Equal(crc32OriginalValue, storedCrc32Value);
            File.Delete(fileToTransfer);

            var ipfsStream = dfs.ReadAsync(dfsHash).Result;
            b = new byte[byteSize];
            ipfsStream.Read(b, 0, (int) ipfsStream.Length);

            crc32.Reset();
            crc32.Update(b);
            var ipfsCrcValue = crc32.Value;
            Assert.Equal(crc32OriginalValue, ipfsCrcValue);
        }

        #region IDisposable Support

        private void Dispose(bool disposing)
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
