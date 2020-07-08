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
using Catalyst.Abstractions.KeySigner;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using Serilog;

namespace Catalyst.Core.Lib.IO.LibP2PHandlers
{
    public sealed class ProtocolMessageSignHandler : IOutboundMessageHandler
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IKeySigner _keySigner;
        private readonly SigningContext _signingContext;

        public ProtocolMessageSignHandler(IKeySigner keySigner, SigningContext signingContext)
        {
            _keySigner = keySigner;
            _signingContext = signingContext;
        }

        /// <summary>
        ///     Signs a protocol message, or straight WriteAndFlush non-protocolMessages
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task<bool> ProcessAsync(ProtocolMessage message)
        {
            Logger.Verbose("Signing message {message}", message);
            message.Sign(_keySigner, _signingContext);
            return Task.FromResult(true);
        }
    }
}
