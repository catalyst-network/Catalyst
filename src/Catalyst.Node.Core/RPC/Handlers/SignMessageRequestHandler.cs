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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using NSec.Cryptography;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class SignMessageRequestHandler
        : CorrelatableMessageHandlerBase<SignMessageRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdentifier _peerIdentifier;

        public SignMessageRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IKeySigner keySigner,
            IMessageCorrelationCache messageCorrelationCache)
            : base(messageCorrelationCache, logger)
        {
            _keySigner = keySigner;
            _peerIdentifier = peerIdentifier;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull();

            Logger.Debug("received message of type SignMessageRequest");

            try
            {
                var deserialised = message.Payload.FromAnySigned<SignMessageRequest>();

                Guard.Argument(message).NotNull("The request cannot be null");

                var decodedMessage = deserialised.Message.ToString(Encoding.UTF8);

                var privateKey = _keySigner.CryptoContext.GeneratePrivateKey();

                var signature = _keySigner.CryptoContext.Sign(privateKey, Encoding.UTF8.GetBytes(decodedMessage));

                Guard.Argument(signature).NotNull("Failed to sign message. The signature cannot be null.");

                var publicKey = _keySigner.CryptoContext.GetPublicKey(privateKey);

                Guard.Argument(publicKey).NotNull("Failed to get the public key.  Public key cannot be null.");

                Logger.Debug("message content is {0}", deserialised.Message);

                var response = new RpcMessageFactory<SignMessageResponse>(CorrelationCache).GetMessage(
                    new SignMessageResponse
                    {
                        OriginalMessage = deserialised.Message,
                        PublicKey = publicKey.GetNSecFormatPublicKey().Export(KeyBlobFormat.PkixPublicKey)
                           .ToByteString(),
                        Signature = signature.ToByteString()
                    },
                    new PeerIdentifier(message.Payload.PeerId),
                    _peerIdentifier,
                    MessageTypes.Tell,
                    message.Payload.CorrelationId.ToGuid());

                message.Context.Channel.WriteAndFlushAsync(response).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle SignMessageRequest after receiving message {0}", message);
                throw;
            }
        }
    }
}
