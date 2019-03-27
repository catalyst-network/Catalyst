

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
    public class GetVersionResponseHandler : MessageHandlerBase<VersionResponse>
    {
        //private readonly IRpcNodesSettings _config;

        public GetVersionResponseHandler(
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
                Logger.Debug("Handling GetVersionResponse");
                
                var deserialised = message.Payload.FromAny<VersionResponse>();
                Console.WriteLine("Node Version: {0}", deserialised.Version.ToString());
                Console.WriteLine("Press Enter to continue ...\n");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoResponse after receiving message {0}", message);
                throw ex;
            }
        }
    }
}