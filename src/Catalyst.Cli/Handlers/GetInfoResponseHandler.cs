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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// Handler responsible for handling the server's response for the GetInfo request.
    /// The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public class GetInfoResponseHandler : MessageHandlerBase<GetInfoResponse>
    {
        /// <summary>
        /// Constructor. Calls the base class <see cref="MessageHandlerBase"/> constructor.
        /// </summary>
        /// <param name="messageStream">The message stream the handler is listening to through which the handler will
        /// receive the response from the server.</param>
        /// <param name="logger">Logger to log debug related information.</param>
        public GetInfoResponseHandler(IObservable<IChanneledMessage<AnySigned>> messageStream,
            ILogger logger)
            : base(messageStream, logger) { }

        /// <summary>
        /// Handles the VersionResponse message sent from the <see cref="GetInfoResponseHandler" />.
        /// </summary>
        /// <param name="message">An object of GetInfoResponse</param>
        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            if (message == NullObjects.ChanneledAnySigned)
            {
                return;
            }

            try
            {
                Logger.Debug("Handling GetInfoResponse");

                var deserialised = message.Payload.FromAnySigned<GetInfoResponse>();

                var result = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(deserialised.Query);

                Console.WriteLine(@"[");

                WriteConfiguration(result, 0, result.Count);

                Console.WriteLine(@"]");

                Console.WriteLine(@"Press Enter to continue ...");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoResponse after receiving message {0}", message);
                throw;
            }
        }

        private void WriteConfiguration(IReadOnlyList<KeyValuePair<string, string>> configList, int startIndex, int count)
        {
            var childIndex = 0;
            var childrenCount = 0;

            Console.WriteLine(@"	");
            for (var j = startIndex; j < startIndex + count; j++)
            {
                if (j < childIndex + childrenCount)
                {
                    continue;
                }

                var setting = configList[j];
                var key = setting.Key;
                var value = setting.Value ?? "";

                if (value.Contains("subsection"))
                {
                    childIndex = j + 1;
                    childrenCount = Convert.ToInt16(value.Substring(value.IndexOf('_') + 1));

                    if (!configList.First().Equals(setting) && !configList[childIndex].Key.Contains("0"))
                    {
                        Console.WriteLine(@"},");
                        Console.WriteLine(@"" + key + @": {");
                    }
                    else
                    {
                        Console.WriteLine(@"" + key + @": [");
                    }

                    WriteConfiguration(configList, childIndex, childrenCount);
                }
                else
                {
                    Console.WriteLine(@"        {0}: {1},", key, value);
                }
            }
        }
    }
}
