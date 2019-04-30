using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// The response handler for removing a peer
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{RemovePeerResponse, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcResponseHandler" />
    public sealed class RemovePeerResponseHandler
        : CorrelatableMessageHandlerBase<RemovePeerResponse, IMessageCorrelationCache>,
            IRpcResponseHandler
    {
        /// <summary>The user output</summary>
        private readonly IUserOutput _userOutput;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerResponseHandler"/> class.</summary>
        /// <param name="userOutput">The user output.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        public RemovePeerResponseHandler(IUserOutput userOutput,
            IMessageCorrelationCache correlationCache,
            ILogger logger) : base(correlationCache, logger)
        {
            _userOutput = userOutput;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Logger.Debug("Handling Remove Peer Response");

            Guard.Argument(message).NotNull("Received message cannot be null");

            try
            {
                var deserialised = message.Payload.FromAnySigned<RemovePeerResponse>();
                _userOutput.WriteLine("Deleted peer: " + deserialised.Deleted);
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle RemovePeerResponse after receiving message {0}", message);
                throw;
            }
        }
    }
}
