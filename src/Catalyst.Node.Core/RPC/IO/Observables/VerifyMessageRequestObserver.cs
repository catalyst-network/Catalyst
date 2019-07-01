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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Nethereum.RLP;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.IO.Observables
{
    public sealed class VerifyMessageRequestObserver
        : RequestObserverBase<VerifyMessageRequest, VerifyMessageResponse>, IRpcRequestObserver
    {
        private readonly IKeySigner _keySigner;

        private const string PublicKeyInvalid = "Invalid PublicKey";
        private const string SignatureInvalid = "Invalid Signature";
        private const string FailedToHandleMessage = "Failed to handle VerifyMessageRequest after receiving message";

        public VerifyMessageRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IKeySigner keySigner)
            : base(logger, peerIdentifier)
        {
            _keySigner = keySigner;
        }

        protected override VerifyMessageResponse HandleRequest(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received message of type VerifyMessageRequest");

            var deserialised = messageDto.Payload.FromProtocolMessage<VerifyMessageRequest>();
            Guard.Argument(deserialised).NotNull("The verify message request cannot be null");

            var decodedMessage = RLP.Decode(deserialised.Message.ToByteArray()).RLPData;
            var decodedPublicKey = RLP.Decode(deserialised.PublicKey.ToByteArray()).RLPData;
            var decodedSignature = RLP.Decode(deserialised.Signature.ToByteArray()).RLPData;

            IPublicKey publicKey = null;
            try
            {
                publicKey = _keySigner.CryptoContext.ImportPublicKey(decodedPublicKey);

                Guard.Argument(publicKey).HasValue();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{PublicKeyInvalid} {messageDto}");
            }

            ISignature signature = null;
            try
            {
                signature = new Signature(decodedSignature);
                Guard.Argument(signature).HasValue();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{SignatureInvalid} {messageDto}");
            }

            try
            {
                var result = _keySigner.CryptoContext.Verify(publicKey, decodedMessage, signature);

                Logger.Debug("message content is {0}", deserialised.Message);
                
                return ReturnResponse(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{FailedToHandleMessage} {messageDto}");
                throw;
            } 
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
