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
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Org.BouncyCastle.Math.EC;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Catalyst.Cli
{
    public class GetVersionResponseHandler : MessageHandlerBase<VersionResponse>
    {
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
                Logger.Information("Node Version: {0}", deserialised.Version.ToString());
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