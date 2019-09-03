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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.IO.Observers;
using Catalyst.Core.Util;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    public sealed class SignMessageRequestObserver
        : RequestObserverBase<SignMessageRequest, SignMessageResponse>,
            IRpcRequestObserver
    {
        private readonly IKeySigner _keySigner;

        public SignMessageRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IKeySigner keySigner)
            : base(logger, peerIdentifier)
        {
            _keySigner = keySigner;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signMessageRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override SignMessageResponse HandleRequest(SignMessageRequest signMessageRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(signMessageRequest, nameof(signMessageRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("received message of type SignMessageRequest");

            var decodedMessage = signMessageRequest.Message.ToByteArray();
            var signingContext = signMessageRequest.SigningContext;

            var signature = _keySigner.Sign(decodedMessage, signingContext);

            var publicKey = _keySigner.CryptoContext.PublicKeyFromBytes(signature.PublicKeyBytes);

            Guard.Argument(signature).NotNull("Failed to sign message. The signature cannot be null.");

            Guard.Argument(publicKey).NotNull("Failed to get the public key. Public key cannot be null.");

            Logger.Debug("message content is {0}", signMessageRequest.Message);

            return new SignMessageResponse
            {
                OriginalMessage = signMessageRequest.Message,
                PublicKey = signature.PublicKeyBytes.ToByteString(),
                Signature = signature.SignatureBytes.ToByteString()
            };
        }
    }
}
