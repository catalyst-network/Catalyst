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

using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Protocol.Wire;
using Serilog;

namespace Catalyst.Core.Lib.IO.Handlers
{
    public sealed class CorrelationHandler<T> : IInboundMessageHandler where T : IMessageCorrelationManager
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly T _messageCorrelationManager;

        public CorrelationHandler(T messageCorrelationManager)
        {
            _messageCorrelationManager = messageCorrelationManager;
        }

        /// <summary>
        ///     The server should always correlate a response, if it can fire next pipeline, if not close the channel,
        ///     If the message is not a response (IE Request/Broadcast) it should pass on to the next handler without attempting to correlate.
        /// </summary>
        /// <param name="message"></param>
        public Task<bool> ProcessAsync(ProtocolMessage message)
        {
            Logger.Verbose("Received {message}", message);
            if (message.TypeUrl.EndsWith(MessageTypes.Response.Name))
            {
                if (_messageCorrelationManager.TryMatchResponse(message))
                {
                    return Task.FromResult(true);
                }
            }
            else
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
