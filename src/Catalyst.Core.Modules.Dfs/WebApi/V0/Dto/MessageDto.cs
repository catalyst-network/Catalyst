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
using Lib.P2P.PubSub;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Dto
{
    /// <summary>
    ///     A published message.
    /// </summary>
    internal sealed class MessageDto
    {
        /// <summary>
        ///     The base-64 encoding of the author's peer id.
        /// </summary>
        public string From;

        /// <summary>
        ///     The base-64 encoding of the author's unique sequence number.
        /// </summary>
        public string Seqno;

        /// <summary>
        ///     The base-64 encoding of the message data.
        /// </summary>
        public string Data;

        /// <summary>
        ///     The topics associated with the message.
        /// </summary>
        public string[] TopicIDs;

        /// <summary>
        ///     Create a new instance of the <see cref="MessageDto" />
        ///     from the <see cref="IPublishedMessage" />.
        /// </summary>
        /// <param name="msg">
        ///     A pubsub messagee.
        /// </param>
        public MessageDto(IPublishedMessage msg)
        {
            From = Convert.ToBase64String(msg.Sender.Id.ToArray());
            Seqno = Convert.ToBase64String(msg.SequenceNumber);
            Data = Convert.ToBase64String(msg.DataBytes);
            TopicIDs = msg.Topics.ToArray();
        }
    }
}
