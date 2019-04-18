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
using Multiformats.Base;
using Nethereum.RLP;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class VerifyMessageRequestHandler : MessageHandlerBase<VerifyMessageRequest>, IRpcRequestHandler
    {
        private readonly IKeySigner _keySigner;
        private readonly PeerId _peerId;

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
            Logger.Debug("received message of type VerifyMessageRequest");
            
            var deserialised = message.Payload.FromAnySigned<VerifyMessageRequest>();

            var decodedMessage = RLP.Decode(deserialised.Message.ToByteArray())[0].RLPData.ToStringFromRLPDecoded();
            var publicKey = deserialised.PublicKey;
            var signature = deserialised.Signature;

            bool result = true;

            try
            {
                MultibaseEncoding encodingUsed;

                if (!Multibase.TryDecode(publicKey.ToStringUtf8(), out encodingUsed, out byte[] decodedPublicKey))
                {
                    Logger.Error("PublicKey passed has an unknown encoding.");
                    result = false;
                }

                if (!Multibase.TryDecode(signature.ToStringUtf8(), out encodingUsed, out byte[] decodedSignature))
                {
                    Logger.Error("Signature passed has an unknown encoding.");
                    result = false;
                }

                if (result)
                {
                    IPublicKey pubKey = _keySigner.CryptoContext.ImportPublicKey(decodedPublicKey);

                    result = _keySigner.CryptoContext.Verify(pubKey, decodedMessage.ToBytesForRLPEncoding(),
                        decodedSignature);

                    Logger.Debug("message content is {0}", deserialised.Message);
                }

                var response = new VerifyMessageResponse
                {
                    IsSignedByKey = result
                };

                var anySignedResponse = response.ToAnySigned(_peerId, message.Payload.CorrelationId.ToGuid());

                message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle VerifyMessageRequest after receiving message {0}", message);
                throw;
            } 
        }
    }
}
