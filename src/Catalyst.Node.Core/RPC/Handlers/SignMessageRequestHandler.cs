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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Nethereum.KeyStore.Crypto;
using Nethereum.RLP;
using NSec.Cryptography;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class SignMessageRequestHandler : MessageHandlerBase<SignMessageRequest>
    {
        private readonly IKeySigner _keySigner;
        private readonly PeerId _peerId;

        public SignMessageRequestHandler(IObservable<IChanneledMessage<AnySigned>> messageStream,
            IPeerIdentifier peerIdentifier,
            ILogger logger,
            IKeySigner keySigner)
            : base(messageStream, logger)
        {
            _keySigner = keySigner;
            _peerId = peerIdentifier.PeerId;
        }

        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            if (message == NullObjects.ChanneledAnySigned)
            {
                return;
            }

            Logger.Debug("received message of type SignMessageRequest");
            try
            {
                var deserialised = message.Payload.FromAnySigned<SignMessageRequest>();

                //decode the received message
                var decodeResult = RLP.Decode(deserialised.Message.ToByteArray())[0].RLPData;

                //get the original message from the decoded message
                var decodedMessage = decodeResult.ToStringFromRLPDecoded();

                //use the keysigner to sign the message
                var privateKey = _keySigner.CryptoContext.GeneratePrivateKey();
                var signature = _keySigner.CryptoContext.Sign(privateKey, Encoding.UTF8.GetBytes(decodedMessage));
                var publicKey = _keySigner.CryptoContext.GetPublicKey(privateKey);
                var exportedPublicKey = _keySigner.CryptoContext.ExportPublicKey(publicKey);

                Logger.Debug("message content is {0}", deserialised.Message);
                
                KeyStoreCrypto keyStoreCrypto = new KeyStoreCrypto();

                var response = new SignMessageResponse
                {
                    OriginalMessage = deserialised.Message,
                    PublicKey = publicKey.GetNSecFormatPublicKey().Export(KeyBlobFormat.PkixPublicKey).ToByteString(),
                    Signature = signature.ToByteString()
                };

                var anySignedResponse = response.ToAnySigned(_peerId, message.Payload.CorrelationId.ToGuid());
                message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
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
