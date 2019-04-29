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

using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;
using Catalyst.Common.Extensions;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Common.Config;
using Catalyst.Common.FileSystem;
using System;
using System.IO;
using Catalyst.Common.Rpc;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Node.Core.P2P.Messaging;
using System.Net;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Google.Protobuf;
using NSubstitute.Extensions;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// The request handler to add a file to the DFS
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{AddFileToDfsRequest, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public class AddFileToDfsRequestHandler : CorrelatableMessageHandlerBase<AddFileToDfsRequest, IMessageCorrelationCache>,
        IRpcRequestHandler
    {
        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactoryBase<AddFileToDfsResponse, RpcMessages> _rpcMessageFactory;

        /// <summary>The file transfer</summary>
        private readonly IFileTransfer _fileTransfer;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The DFS</summary>
        private readonly IDfs _dfs;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestHandler"/> class.</summary>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="dfs">The DFS.</param>
        public AddFileToDfsRequestHandler(IDfs dfs, IPeerIdentifier peerIdentifier, IFileTransfer fileTransfer, IMessageCorrelationCache correlationCache, ILogger logger) : base(correlationCache, logger)
        {
            _rpcMessageFactory = new RpcMessageFactoryBase<AddFileToDfsResponse, RpcMessages>();
            _fileTransfer = fileTransfer;
            _dfs = dfs;
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<AddFileToDfsRequest>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");

            uint chunkSize = (uint) Math.Max(1, (int) Math.Ceiling((double) deserialised.FileSize / FileTransferConstants.ChunkSize));

            FileTransferInformation fileTransferInformation = new FileTransferInformation(message.Context.Channel, message.Payload.CorrelationId.ToGuid().ToString(), deserialised.FileName, chunkSize);
            fileTransferInformation.OnSuccess += OnSuccess;

            AddFileToDfsResponseCode responseCode;
            try
            {
                responseCode = _fileTransfer.InitializeTransfer(fileTransferInformation);
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                responseCode = AddFileToDfsResponseCode.Error;
            }

            ReturnResponse(fileTransferInformation, responseCode);
        }

        /// <summary>Called when [success] on file transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        private void OnSuccess(FileTransferInformation fileTransferInformation)
        {
            AddFileToDfsResponseCode responseCode = AddFileToDfsResponseCode.Finished;

            try
            {
                fileTransferInformation.RandomAccessStream.Seek(0, SeekOrigin.Begin);
                var fileHash = _dfs.AddAsync(fileTransferInformation.RandomAccessStream.BaseStream, fileTransferInformation.FileName).Result;
                responseCode = AddFileToDfsResponseCode.Successful;
                fileTransferInformation.DfsHash = fileHash;

                Logger.Information($"Added File Name {fileTransferInformation.FileName} to DFS, Hash: {fileHash}");
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                responseCode = AddFileToDfsResponseCode.Failed;
            }
            finally
            {
                ReturnResponse(fileTransferInformation, responseCode);
                fileTransferInformation.CleanUp();
            }
        }

        /// <summary>Returns the response.</summary>
        /// <param name="message">The message sent by client</param>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="fileSize">Size of the file.</param>
        private void ReturnResponse(FileTransferInformation fileTransferInformation, AddFileToDfsResponseCode responseCode)
        {
            Logger.Information("File transfer response code: " + responseCode);
            if (responseCode == AddFileToDfsResponseCode.Successful)
            {
                Logger.Information($"Initialised file transfer, FileName: {fileTransferInformation.FileName}, Chunks: {fileTransferInformation.MaxChunk}");
            }

            var dfsHash = responseCode == AddFileToDfsResponseCode.Finished ? fileTransferInformation.DfsHash : string.Empty;

            // Build Response
            AddFileToDfsResponse response = new AddFileToDfsResponse
            {
                ResponseCode = ByteString.CopyFrom((byte) responseCode),
                DfsHash = dfsHash
            };

            // Send Response
            var responseMessage = _rpcMessageFactory.GetMessage(new P2PMessageDto<AddFileToDfsResponse, RpcMessages>(
                type: RpcMessages.AddFileToDfsResponse,
                message: response,
                destination: (IPEndPoint) fileTransferInformation.ReciepientChannel.RemoteAddress,
                sender: _peerIdentifier
            ));

            fileTransferInformation.ReciepientChannel.WriteAndFlushAsync(responseMessage);
        }
    }
}
