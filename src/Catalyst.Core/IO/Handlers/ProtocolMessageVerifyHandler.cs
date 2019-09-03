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

using System.Reflection;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.IO.Handlers
{
    public sealed class ProtocolMessageVerifyHandler : InboundChannelHandlerBase<ProtocolMessageSigned>
    {
        private readonly IKeySigner _keySigner;
        private readonly SigningContext _signingContext;

        public ProtocolMessageVerifyHandler(IKeySigner keySigner, ISigningContextProvider signingContextProvider)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            _keySigner = keySigner;
            _signingContext = new SigningContext
            {
                Network = signingContextProvider.Network,
                SignatureType = signingContextProvider.SignatureType 
            };
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessageSigned signedMessage)
        {
            Logger.Verbose("Received {msg}", signedMessage);
            if (!Verify(signedMessage))
            {
                Logger.Warning("Failed to verify {msg} signature.", signedMessage);
                return;
            }

            if (signedMessage.Message.IsBroadCastMessage())
            {
                var innerSignedMessage = ProtocolMessageSigned.Parser.ParseFrom(signedMessage.Message.Value);
                if (!Verify(innerSignedMessage))
                {
                    Logger.Warning("Failed to verify inner signature in broadcast message {msg}.", innerSignedMessage);
                    return;
                }
            }

            ctx.FireChannelRead(signedMessage.Message);
        }

        private bool Verify(ProtocolMessageSigned signedMessage)
        {
            var sig = signedMessage.Signature.ToByteArray();
            var pub = signedMessage.Message.PeerId.PublicKey.ToByteArray();
            var signature = _keySigner.CryptoContext.SignatureFromBytes(sig, pub);
            return _keySigner.Verify(signature, signedMessage.Message.ToByteArray(), _signingContext);
        }
    }
}
