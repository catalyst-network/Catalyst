using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Rpc;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Catalyst.Node.Core.RPC.Handlers
{
    class TransferFileBytesRequestHandler : CorrelatableMessageHandlerBase<TransferFileBytesRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        private IFileTransfer _fileTransfer;
        private RpcMessageFactoryBase<TransferFileBytesResponse, RpcMessages> _rpcMessageFactory;
        private IPeerIdentifier _peerIdentifier;

        public TransferFileBytesRequestHandler(IFileTransfer fileTransfer, IPeerIdentifier peerIdentifier, IMessageCorrelationCache correlationCache, ILogger logger) : base(correlationCache, logger)
        {
            _fileTransfer = fileTransfer;
            _rpcMessageFactory = new RpcMessageFactoryBase<TransferFileBytesResponse, RpcMessages>();
            _peerIdentifier = peerIdentifier;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<TransferFileBytesRequest>();

            AddFileToDfsResponseCode responseCode;

            try
            {
                Guard.Argument(deserialised).NotNull("Message cannot be null");

                Guid correlationId = new Guid(deserialised.CorrelationFileName.ToByteArray());
                responseCode = _fileTransfer.WriteChunk(correlationId.ToString(), deserialised.ChunkId, deserialised.ChunkBytes.ToByteArray());
            } catch(Exception e)
            {
                Logger.Error(e.ToString());
                responseCode = AddFileToDfsResponseCode.Error;
            }

            TransferFileBytesResponse responseMessage = new TransferFileBytesResponse();
            responseMessage.ResponseCode = ByteString.CopyFrom((byte)responseCode);

            var responseDto = _rpcMessageFactory.GetMessage(new P2PMessageDto<TransferFileBytesResponse, RpcMessages>(
                type: RpcMessages.TransferFileBytesResponse,
                message: responseMessage,
                destination: (IPEndPoint) message.Context.Channel.RemoteAddress,
                sender: _peerIdentifier
            ));
            message.Context.Channel.WriteAndFlushAsync(responseDto);
        }
    }
}
