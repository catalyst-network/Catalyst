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

// #region LICENSE
//
// /**
// * Copyright (c) 2019 Catalyst Network
// *
// * This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
// *
// * Catalyst.Node is free software: you can redistribute it and/or modify
// * it under the terms of the GNU General Public License as published by
// * the Free Software Foundation, either version 2 of the License, or
// * (at your option) any later version.
// *
// * Catalyst.Node is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// * GNU General Public License for more details.
// *
// * You should have received a copy of the GNU General Public License
// * along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
// */
//
// #endregion
//
// using System;
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;
// using Catalyst.Abstractions.Cryptography;
// using Catalyst.Abstractions.Dfs;
// using Catalyst.Abstractions.FileTransfer;
// using Catalyst.Abstractions.Rpc;
// using Catalyst.Abstractions.Types;
// using Catalyst.Core.Lib.Cryptography;
// using Catalyst.Core.Lib.Extensions;
// using Catalyst.Core.Lib.FileTransfer;
// using Catalyst.Core.Lib.IO.Messaging.Correlation;
// using Catalyst.Core.Modules.Hashing;
// using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
// using Catalyst.Protocol.Rpc.Node;
// using Catalyst.TestUtils;
// using DotNetty.Transport.Channels;
// using MultiFormats.Registry;
// using NSubstitute;
// using Serilog;
// using NUnit.Framework;
// 
//
// namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
// {
//     public sealed class NodeFileTransferTests : FileSystemBasedTest
//     {
//         private readonly ILogger _logger;
//         private readonly IChannelHandlerContext _fakeContext;
//         private readonly IDownloadFileTransferFactory _nodeFileTransferFactory;
//         private readonly IDfsService _dfsService;
//
//         public NodeFileTransferTests(TestContext testOutput) : base(testOutput)
//         {
//             _logger = Substitute.For<ILogger>();
//             _fakeContext = Substitute.For<IChannelHandlerContext>();
//             _nodeFileTransferFactory = new DownloadFileTransferFactory(_logger);
//
//             var passwordManager = Substitute.For<IPasswordManager>();
//             passwordManager
//                .RetrieveOrPromptAndAddPasswordToRegistry(PasswordRegistryTypes.DefaultNodePassword, Arg.Any<string>())
//                .Returns(TestPasswordReader.BuildSecureStringPassword("abcd"));
//
//             var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
//             var testPasswordManager = new PasswordManager(new TestPasswordReader(), new PasswordRegistry());
//             _dfsService = new DfsService(hashProvider, testPasswordManager);
//         }
//
//         [Test]
//        
//         public async Task Cancel_File_Transfer()
//         {
//             var sender = MultiAddressHelper.GetAddress("sender");
//
//             IDownloadFileInformation fileTransferInformation = new DownloadFileTransferInformation(
//                 sender,
//                 sender,
//                 _fakeContext.Channel,
//                 CorrelationId.GenerateCorrelationId(),
//                 string.Empty,
//                 555);
//
//             var cancellationTokenSource = new CancellationTokenSource();
//             _nodeFileTransferFactory.RegisterTransfer(fileTransferInformation);
//             _nodeFileTransferFactory
//                .FileTransferAsync(fileTransferInformation.CorrelationId, cancellationTokenSource.Token)
//                .ConfigureAwait(false).GetAwaiter();
//             Assert.Single(_nodeFileTransferFactory.Keys);
//             cancellationTokenSource.Cancel();
//
//             await TaskHelper.WaitForAsync(() => fileTransferInformation.IsCompleted, TimeSpan.FromSeconds(10));
//
//             var fileCleanedUp = !File.Exists(fileTransferInformation.TempPath);
//
//             Assert.True(fileTransferInformation.IsExpired());
//             Assert.True(fileCleanedUp);
//             Assert.Empty(_nodeFileTransferFactory.Keys);
//         }
//
//         [Theory]
//        
//         [TestCase(1000L)]
//         [TestCase(82000L)]
//         [TestCase(100000L)]
//         public async Task Verify_File_Integrity_On_Transfer(long byteSize)
//         {
//             await AddFileToDfs(byteSize).ConfigureAwait(false);
//         }
//
//         private async Task AddFileToDfs(long byteSize)
//         {
//             var fakeNode = Substitute.For<IRpcClient>();
//             var sender = MultiAddressHelper.GetAddress("sender");
//             var recipient = MultiAddressHelper.GetAddress("recipient");
//             var sender = sender;
//             var peerSettings = sender.ToSubstitutedPeerSettings();
//             var recipientPeerId = recipient;
//             var fileToTransfer = FileHelper.CreateRandomTempFile(byteSize);
//             var addFileToDfsRequestHandler =
//                 new AddFileToDfsRequestObserver(_dfsService, peerSettings, _nodeFileTransferFactory, _logger);
//             var transferBytesRequestHandler =
//                 new TransferFileBytesRequestObserver(_nodeFileTransferFactory, peerSettings, _logger);
//
//             var uniqueFileKey = CorrelationId.GenerateCorrelationId();
//             var crcValue = FileHelper.GetCrcValue(fileToTransfer);
//
//             //Create a response object and set its return value
//             var request = new AddFileToDfsRequest
//             {
//                 Node = "node1",
//                 FileName = fileToTransfer,
//                 FileSize = (ulong) byteSize
//             }.ToProtocolMessage(sender, uniqueFileKey);
//             request.SendToHandler(_fakeContext, addFileToDfsRequestHandler);
//
//             Assert.Single(_nodeFileTransferFactory.Keys);
//
//             var fileTransferInformation =
//                 _nodeFileTransferFactory.GetFileTransferInformation(uniqueFileKey);
//             Assert.True(fileTransferInformation.Initialised, "File transfer not initialised");
//
//             using (var fs = File.Open(fileToTransfer, FileMode.Open))
//             {
//                 var fileUploadInformation = new UploadFileTransferInformation(fs, sender, recipientPeerId,
//                     fakeNode.Channel, uniqueFileKey);
//                 for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
//                 {
//                     fileUploadInformation.GetUploadMessageDto(i).Content
//                        .SendToHandler(_fakeContext, transferBytesRequestHandler);
//                 }
//             }
//
//             Assert.True(fileTransferInformation.ChunkIndicatorsTrue());
//
//             await TaskHelper.WaitForAsync(() => fileTransferInformation.DfsHash != null, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
//             Assert.NotNull(fileTransferInformation.DfsHash);
//
//             long ipfsCrcValue;
//             using (var ipfsStream = await _dfsService.FileSystem.ReadFileAsync(fileTransferInformation.DfsHash).ConfigureAwait(false))
//             {
//                 ipfsCrcValue = FileHelper.GetCrcValue(ipfsStream);
//             }
//
//             Assert.AreEqual(crcValue, ipfsCrcValue);
//         }
//     }
// }
