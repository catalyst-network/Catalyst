

using System;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Org.BouncyCastle.Math.EC;
using ILogger = Serilog.ILogger;

namespace Catalyst.Cli
{
    public class GetInfoResponseHandler : MessageHandlerBase<GetInfoResponse>
    {
        public GetInfoResponseHandler(
            IObservable<IChanneledMessage<Any>> messageStream,
            ILogger logger)
            : base(messageStream, logger)
        {
            
        }

        public override void HandleMessage(IChanneledMessage<Any> message)
        {
            if (message == NullObjects.ChanneledAny)
            {
                return;
            }
            
            try
            {
                Logger.Debug("Handling GetInfoResponse");
                var deserialised = message.Payload.FromAny<GetInfoResponse>();
                Logger.Information("Requested node configuration\n============================\n{0}", deserialised.Query.ToString());
                Logger.Information("Press Enter to continue ...\n");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoResponse after receiving message {0}", message);
                throw;
            }
        }
    }
}