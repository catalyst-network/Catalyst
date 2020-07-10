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
using System.Linq;
using System.Reactive.Linq;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Microsoft.Reactive.Testing;

namespace Catalyst.TestUtils
{
    public static class MessageStreamHelper
    {
        public static void SendToHandler(this ProtocolMessage message,
            MessageObserverBase handler)
        {
            handler.OnNext(message);
        }

        //Force test scheduler for testing streams
        public static IObservable<ProtocolMessage> CreateStreamWithMessage(TestScheduler testScheduler,
            ProtocolMessage response)
        {
            return new[] { response }.ToObservable(testScheduler);
        }

        public static IObservable<ProtocolMessage> CreateStreamWithMessages<T>(TestScheduler testScheduler,
            params T[] messages)
            where T : IMessage<T>, IMessage
        {
            var protoMessages = messages.Select(m => m.ToProtocolMessage(MultiAddressHelper.GetAddress(), CorrelationId.GenerateCorrelationId()));

            return CreateStreamWithMessages(testScheduler, protoMessages.ToArray());
        }

        public static IObservable<ProtocolMessage> CreateStreamWithMessages(TestScheduler testScheduler,
            params ProtocolMessage[] responseMessages)
        {
            return responseMessages.ToObservable(testScheduler);;
        }
    }
}
