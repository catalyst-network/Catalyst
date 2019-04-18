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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Multiformats.Base;
using Nethereum.RLP;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class VerifyMessageRequestHandler : MessageHandlerBase<VerifyMessageRequest>, IRpcRequestHandler
    {
        private readonly IKeySigner _keySigner;
        private readonly PeerId _peerId;
        private IChanneledMessage<AnySigned> _message;

        public VerifyMessageRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IKeySigner keySigner)
            : base(logger)
        {
            _keySigner = keySigner;
            _peerId = peerIdentifier.PeerId;
        }

        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            _message = message;
            
            Logger.Debug("received message of type VerifyMessageRequest");
            
            var deserialised = message.Payload.FromAnySigned<VerifyMessageRequest>();

            var decodedMessage = RLP.Decode(deserialised.Message.ToByteArray())[0].RLPData.ToStringFromRLPDecoded();
            var publicKey = deserialised.PublicKey;
            var signature = deserialised.Signature;

            try
            {
                if (!Multibase.TryDecode(publicKey.ToStringUtf8(), out var encodingUsed, out var decodedPublicKey))
                {
                    Logger.Error($"Invalid PublicKey encoding {encodingUsed}.");
                    ReturnResponse(false);
                    return;
                }

                if (decodedPublicKey.Length == 0)
                {
                    Logger.Error($"PublicKey not provided.");
                    ReturnResponse(false);
                    return;
                }
                
                if (!Multibase.TryDecode(signature.ToStringUtf8(), out encodingUsed, out var decodedSignature))
                {
                    Logger.Error($"Invalid Signature encoding. {encodingUsed}.");
                    ReturnResponse(false);
                    return;
                }
                
                if (decodedSignature.Length == 0)
                {
                    Logger.Error($"Signature not provided.");
                    ReturnResponse(false);
                    return;
                }
                
                var pubKey = _keySigner.CryptoContext.ImportPublicKey(decodedPublicKey);

                Guard.Argument(pubKey).HasValue();

                var result = _keySigner.CryptoContext.Verify(pubKey, decodedMessage.ToBytesForRLPEncoding(),
                    decodedSignature);

                Logger.Debug("message content is {0}", deserialised.Message);
                
                ReturnResponse(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle VerifyMessageRequest after receiving message {0}", message);
                throw;
            } 
        }

        private void ReturnResponse(bool result)
        {
            var response = new VerifyMessageResponse
            {
                IsSignedByKey = result
            };

            var anySignedResponse = response.ToAnySigned(_peerId, _message.Payload.CorrelationId.ToGuid());

            _message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
        }
    }
}
