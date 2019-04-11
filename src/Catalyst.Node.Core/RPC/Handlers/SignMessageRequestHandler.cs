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
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf.WellKnownTypes;
using Nethereum.RLP;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public class SignMessageRequestHandler : MessageHandlerBase<SignMessageRequest>
    {
        private readonly IKeySigner _keySigner;

        public SignMessageRequestHandler(
            IObservable<IChanneledMessage<Any>> messageStream,
            ILogger logger,
            IKeySigner keySigner)
            : base(messageStream, logger)
        {
            _keySigner = keySigner;
        }

        public override void HandleMessage(IChanneledMessage<Any> message)
        {
            if(message == NullObjects.ChanneledAny) {return;}
            Logger.Debug("received message of type SignMessageRequest");
            try
            {
                var deserialised = message.Payload.FromAny<SignMessageRequest>();

                //decode the received message
                var decodeResult = Nethereum.RLP.RLP.Decode(deserialised.Message.ToByteArray())[0].RLPData;

                //get the original message from the decoded message
                var originalMessage = decodeResult.ToStringFromRLPDecoded();

                //use the keysigner to sign the message
                var privateKey = _keySigner.CryptoContext.GeneratePrivateKey();
                var signature = _keySigner.CryptoContext.Sign(privateKey, Encoding.UTF8.GetBytes(originalMessage));

                //get the public key
                var publicKey = _keySigner.CryptoContext.GetPublicKey(privateKey);

                Logger.Debug("message content is {0}", deserialised.Message);

                var response = new SignMessageResponse
                {
                    Signature = signature.ToByteString(),
                    PublicKey = _keySigner.CryptoContext.ExportPublicKey(publicKey).ToByteString(),
                    OriginalMessage = deserialised.Message
                };

                message.Context.Channel.WriteAndFlushAsync(response.ToAny()).GetAwaiter().GetResult();
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
