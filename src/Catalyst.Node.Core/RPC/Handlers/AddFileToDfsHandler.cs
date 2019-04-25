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

using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;
using Catalyst.Common.Extensions;
using System.IO;
using System.Linq;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Common.Config;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// The request handler to add a file to the DFS
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{AddFileToDfsRequest, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public class AddFileToDfsRequestHandler : CorrelatableMessageHandlerBase<AddFileToDfsRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>The DFS</summary>
        private readonly IDfs _dfs;

        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactoryBase<AddFileToDfsRequest, RpcMessages> _rpcMessageFactory;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsRequestHandler"/> class.</summary>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="dfs">The DFS.</param>
        public AddFileToDfsRequestHandler(IMessageCorrelationCache correlationCache, ILogger logger, IDfs dfs) : base(correlationCache, logger)
        {
            _dfs = dfs;
            this._rpcMessageFactory = new RpcMessageFactoryBase<AddFileToDfsRequest, RpcMessages>();
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull();

            var deserialised = message.Payload.FromAnySigned<AddFileToDfsRequest>();
           
            Guard.Argument(deserialised).NotNull();

           
            _dfs.AddAsync(new MemoryStream());
        }
    }
}
