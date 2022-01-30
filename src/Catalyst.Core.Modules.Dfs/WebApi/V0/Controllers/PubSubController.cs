#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.WebApi.V0.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Controllers
{
    /// <summary>
    ///     Publishing and subscribing to messages on a topic.
    /// </summary>
    public class PubSubController : DfsController
    {
        /// <summary>
        ///     Creates a new controller.
        /// </summary>
        public PubSubController(IDfsService dfs) : base(dfs) { }

        /// <summary>
        ///     List all the subscribed topics.
        /// </summary>
        [HttpGet] [HttpPost] [Route("pubsub/ls")]
        public async Task<PubSubTopicsDto> List()
        {
            return new PubSubTopicsDto
            {
                Strings = await DfsService.PubSubApi.SubscribedTopicsAsync(Cancel)
            };
        }

        /// <summary>
        ///     List all the peers associated with the topic.
        /// </summary>
        /// <param name="arg">
        ///     The topic name or null/empty for "all topics".
        /// </param>
        [HttpGet] [HttpPost] [Route("pubsub/peers")]
        public async Task<PubSubPeersDto> Peers(string arg)
        {
            var topic = string.IsNullOrEmpty(arg) ? null : arg;
            var peers = await DfsService.PubSubApi.PeersAsync(topic, Cancel);
            return new PubSubPeersDto
            {
                Strings = peers.Select(p => p.Id.ToString())
            };
        }

        /// <summary>
        ///     Publish a message to a topic.
        /// </summary>
        /// <param name="arg">
        ///     The first arg is the topic name and second is the message.
        /// </param>
        [HttpGet] [HttpPost] [Route("pubsub/pub")]
        public async Task Publish(string[] arg)
        {
            if (arg.Length != 2)
            {
                throw new ArgumentException("Missing topic and/or message.");
            }
            
            var message = arg[1].Select(c => (byte) c).ToArray();
            await DfsService.PubSubApi.PublishAsync(arg[0], message, Cancel);
        }

        /// <summary>
        ///     Subscribe to messages on the topic.
        /// </summary>
        /// <param name="arg">
        ///     The topic name.
        /// </param>
        [HttpGet] [HttpPost] [Route("pubsub/sub")]
        public async Task Subscribe(string arg)
        {
            await DfsService.PubSubApi.SubscribeAsync(arg, message =>
            {
                // Send the published message to the caller.
                MessageDto dto = new(message);
                StreamJson(dto);
            }, Cancel);

            // Send 200 OK to caller; but do not close the stream
            // so that published messages can be sent.
            Response.ContentType = "application/json";
            Response.StatusCode = 200;
            await Response.Body.FlushAsync();

            // Wait for the caller to cancel.
            try
            {
                await Task.Delay(-1, Cancel);
            }
            catch (TaskCanceledException)
            {
                // eat
            }
        }
    }
}
