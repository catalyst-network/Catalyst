using Catalyst.Cli.FileTransfer;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Rpc;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    public class TransferFileBytesResponseHandler : CorrelatableMessageHandlerBase<TransferFileBytesResponse, IMessageCorrelationCache>,
            IRpcResponseHandler
    {
        public TransferFileBytesResponseHandler(IMessageCorrelationCache correlationCache, ILogger logger) : base(correlationCache, logger)
        {
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<TransferFileBytesResponse>();

            var responseCode = (AddFileToDfsResponseCode) deserialised.ResponseCode[0];

            CliFileTransfer.Instance.FileTransferCallback(responseCode);
        }
    }
}
