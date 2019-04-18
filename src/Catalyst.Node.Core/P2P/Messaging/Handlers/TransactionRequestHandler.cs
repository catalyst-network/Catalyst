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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Protocol.Transaction;
using Serilog;
using Catalyst.Node.Common.Helpers.IO.Messaging.Handlers;
using Catalyst.Node.Common.Interfaces.IO.Inbound;
using Catalyst.Node.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;

namespace Catalyst.Node.Core.P2P.Messaging.Handlers
{
    public sealed class TransactionRequestHandler
        : ReputationAskRequestHandler<Transaction, IReputableCache>,
            IP2PMessageHandler
    {
        public TransactionRequestHandler(IReputableCache reputableCache,
            ILogger logger)
            : base(reputableCache, logger) { }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Logger.Debug("received pong");
            var deserialised = message.Payload.FromAnySigned<Transaction>();
            Logger.Debug("transaction pong is {0}", deserialised.Signature);
        }
    }
}
