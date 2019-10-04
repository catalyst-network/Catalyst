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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public sealed class VerifyMessageRequestObserver
        : RequestObserverBase<VerifyMessageRequest, VerifyMessageResponse>, IRpcRequestObserver
    {
        private readonly IKeySigner _keySigner;

        private const string PublicKeyInvalid = "Invalid PublicKey";
        private const string SignatureInvalid = "Invalid Signature";

        public VerifyMessageRequestObserver(IPeerSettings peerSettings,
            ILogger logger,
            IKeySigner keySigner)
            : base(logger, peerSettings)
        {
            _keySigner = keySigner;
        }

        /// <param name="verifyMessageRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override VerifyMessageResponse HandleRequest(VerifyMessageRequest verifyMessageRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(verifyMessageRequest, nameof(verifyMessageRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Debug("received message of type VerifyMessageRequest");

            var decodedMessage = verifyMessageRequest.Message.ToByteArray();
            var decodedPublicKey = verifyMessageRequest.PublicKey.ToByteArray();
            var decodedSignature = verifyMessageRequest.Signature.ToByteArray();
            var signatureContext = verifyMessageRequest.SigningContext;

            if (decodedPublicKey.Length != _keySigner.CryptoContext.PublicKeyLength)
            {
                Logger.Error("{0} {1}", PublicKeyInvalid, verifyMessageRequest);
                return ReturnResponse(false);
            }

            if (decodedSignature.Length != _keySigner.CryptoContext.SignatureLength)
            {
                Logger.Error("{0} {1}", SignatureInvalid, verifyMessageRequest);
                return ReturnResponse(false);
            }

            ISignature signature = null;
            try
            {
                signature = _keySigner.CryptoContext.GetSignatureFromBytes(decodedSignature, decodedPublicKey);
            }
            catch (Exception e)
            {
                Logger.Error(e, "{0} {1}", SignatureInvalid, verifyMessageRequest);
                return ReturnResponse(false);
            }

            var result = _keySigner.Verify(signature, decodedMessage, signatureContext);

            Logger.Debug("message content is {0}", verifyMessageRequest.Message);

            return ReturnResponse(result);
        }

        private VerifyMessageResponse ReturnResponse(bool result)
        {
            return new VerifyMessageResponse
            {
                IsSignedByKey = result
            };
        }
    }
}
