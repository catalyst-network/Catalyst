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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class SignMessageRequestObserver
        : RequestObserverBase<SignMessageRequest>,
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

        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received message of type SignMessageRequest");

            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<SignMessageRequest>();

                Guard.Argument(messageDto).NotNull("The request cannot be null");

                string decodedMessage = deserialised.Message.ToString(Encoding.UTF8);

                var signature = _keySigner.Sign(Encoding.UTF8.GetBytes(decodedMessage));

                Guard.Argument(signature).NotNull("Failed to sign message. The signature cannot be null.");

                var publicKey = _keySigner.PublicKey;

                Guard.Argument(publicKey).NotNull("Failed to get the public key.  Public key cannot be null.");

                Logger.Debug("message content is {0}", deserialised.Message);

                return new SignMessageResponse
                {
                    OriginalMessage = deserialised.Message,
                    PublicKey = publicKey.Bytes.RawBytes.ToByteString(),
                    Signature = signature.Bytes.RawBytes.ToByteString()
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle SignMessageRequest after receiving message {0}", messageDto);
                throw;
            }
        }
    }
}
