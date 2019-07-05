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
using System.Text;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Common.Util;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Nethereum.RLP;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.IO.Observers
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

            try
            {
                var decodedMessage = RLP.Decode(signMessageRequest.Message.ToByteArray()).RLPData;

                var signature = _keySigner.Sign(decodedMessage);

                var publicKey = _keySigner.CryptoContext.ImportPublicKey(signature.PublicKeyBytes.RawBytes);

                Guard.Argument(signature).NotNull("Failed to sign message. The signature cannot be null.");

                Guard.Argument(publicKey).NotNull("Failed to get the public key.  Public key cannot be null.");

                Logger.Debug("message content is {0}", signMessageRequest.Message);

                return new SignMessageResponse
                {
                    OriginalMessage = RLP.EncodeElement(decodedMessage).ToByteString(),
                    PublicKey = RLP.EncodeElement(publicKey.Bytes.RawBytes).ToByteString(),
                    Signature = RLP.EncodeElement(signature.SignatureBytes.RawBytes).ToByteString()
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle SignMessageRequest after receiving message {0}", signMessageRequest);
                throw;
            }
        }
    }
}
