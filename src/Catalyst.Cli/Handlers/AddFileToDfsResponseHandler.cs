using Catalyst.Cli.FileTransfer;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Rpc;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    public sealed class AddFileToDfsResponseHandler : CorrelatableMessageHandlerBase<AddFileToDfsResponse, IMessageCorrelationCache>,
        IRpcResponseHandler
    {
        public AddFileToDfsResponseHandler(IMessageCorrelationCache correlationCache, ILogger logger) : base(correlationCache, logger)
        {

        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<AddFileToDfsResponse>();

            Guard.Argument(deserialised).NotNull();

            AddFileToDfsResponseCode responseCode = (AddFileToDfsResponseCode) deserialised.ResponseCode[0];

            CliFileTransfer.Instance.InitialiseFileTransferResponseCallback(responseCode);
        }
    }
}
