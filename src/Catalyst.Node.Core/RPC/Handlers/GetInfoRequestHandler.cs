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
using System.Collections.Generic;
using System.Linq;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public class GetInfoRequestHandler : MessageHandlerBase<GetInfoRequest>
    {
        private readonly IRpcServerSettings _config;

        public GetInfoRequestHandler(
            IObservable<IChanneledMessage<Any>> messageStream,
            IRpcServerSettings config,
            ILogger logger)
            : base(messageStream, logger)
        {
            _config = config;
        }

        public override void HandleMessage(IChanneledMessage<Any> message)
        {
            if(message == NullObjects.ChanneledAny) {return;}
            Logger.Debug("received message of type GetInfoRequest");
            try
            {
                var deserialised = message.Payload.FromAny<GetInfoRequest>();
                Logger.Debug("message content is {0}", deserialised);

                var configuration = GetConfiguration(_config.NodeConfig.GetSection("CatalystNodeConfiguration"));
                
                var serializedList = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                
                var response = new GetInfoResponse
                {
                    Query = serializedList
                };

                message.Context.Channel.WriteAndFlushAsync(response.ToAny()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, 
                    "Failed to handle GetInfoRequest after receiving message {0}", message);
                throw;
            }
        }

        /// <summary>
        /// Creates a list of key value pair objects (string,string) of all configuration items in the passed in configuration section
        /// </summary>
        /// <param name="configSection">IConfigurationSection object including configuration read from config files</param>
        /// <returns>a list of (string,string) key value pairs</returns>
        private IList<KeyValuePair<string, string>> GetConfiguration(IConfiguration configSection)
        {
            IList<KeyValuePair<string, string>> settings = new List<KeyValuePair<string, string>>();
            
            foreach (var subSection in configSection.GetChildren())
            {
                if (subSection.GetChildren().Any())
                {
                    var subList = GetConfiguration(subSection);
                    
                    var section = new KeyValuePair<string, string>(subSection.Key, "subsection_"+subList.Count);
                    settings.Add(section);
                    settings = settings.Concat(subList).ToList();
                    continue;
                }
                
                var item = new KeyValuePair<string, string>(subSection.Key, Convert.ToString(subSection.Value));
                settings.Add(item);
            }

            return settings;
        }
    }
}
