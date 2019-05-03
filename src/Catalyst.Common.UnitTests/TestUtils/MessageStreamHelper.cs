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
using System.Reactive.Linq;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.UnitTests.TestUtils 
{
    public static class MessageStreamHelper
    {
        public static IObservable<IChanneledMessage<AnySigned>> CreateStreamWithMessage(IChannelHandlerContext fakeContext, AnySigned response)
        {   
            var channeledAny = new ChanneledAnySigned(fakeContext, response);
            var messageStream = new[] {channeledAny}.ToObservable();
            return messageStream;
        }

        public static IObservable<IChanneledMessage<AnySigned>> CreateStreamWithMessages(IChannelHandlerContext fakeContext, params AnySigned[] responseMessages)
        {
            List<ChanneledAnySigned> stream = new List<ChanneledAnySigned>();
            foreach (var message in responseMessages)
            {
                var channeledAny = new ChanneledAnySigned(fakeContext, message);
                stream.Add(channeledAny);
            }
            
            var messageStream = stream.ToObservable();
            return messageStream;
        }
    }
}
