using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    public class GetFileFromDfsResponseHandler : CorrelatableMessageHandlerBase<GetFileFromDfsResponse, IMessageCorrelationCache>,
        IRpcResponseHandler
    {
        private readonly IUserOutput _userOutput;

        private readonly IFileTransfer _fileTransfer;

        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="GetFileFromDfsResponseHandler"/> class.</summary>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userOutput">The user output.</param>
        public GetFileFromDfsResponseHandler(IMessageCorrelationCache correlationCache,
            ILogger logger,
            IUserOutput userOutput,
            IFileTransfer fileTransfer,
            IConfigurationRoot config) : base(correlationCache, logger)
        {
            _peerIdentifier = Commands.Commands.BuildCliPeerId(config);
            _userOutput = userOutput;
            _fileTransfer = fileTransfer;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<GetFileFromDfsResponse>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");

            var responseCode = Enumeration.GetAll<FileTransferResponseCodes>().First(respCode => respCode.Id == deserialised.ResponseCode[0]);

            if (responseCode == FileTransferResponseCodes.Successful)
            {
                var fileTransferInformation = _fileTransfer.GetFileTransferInformation(message.Payload.CorrelationId.ToGuid());
                fileTransferInformation?.SetLength(deserialised.FileSize);
            }
        }
    }
}
