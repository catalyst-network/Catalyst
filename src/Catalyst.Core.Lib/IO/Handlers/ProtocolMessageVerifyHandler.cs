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
using System.Reflection;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Wire;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;
using SimpleBase;

namespace Catalyst.Core.Lib.IO.Handlers
{
    public sealed class ProtocolMessageVerifyHandler : InboundChannelHandlerBase<ProtocolMessage>
    {
        private readonly IKeySigner _keySigner;

        public ProtocolMessageVerifyHandler(IKeySigner keySigner)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            _keySigner = keySigner;
        }

        //todo
        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessage signedMessage)
        {
            var pub = signedMessage.PeerId.PublicKey;
            var base32 = Base32.Crockford.Encode(Convert.FromBase64String(pub.ToBase64()), false).ToLower();
            Logger.Verbose("Received {msg}", signedMessage);
            if (!Verify(signedMessage))
            {
                Logger.Warning("Failed to verify {msg} signature.", signedMessage);
                return;
            }

            if (signedMessage.IsBroadCastMessage())
            {
                var innerSignedMessage = ProtocolMessage.Parser.ParseFrom(signedMessage.Value);
                if (!Verify(innerSignedMessage))
                {
                    Logger.Warning("Failed to verify inner signature in broadcast message {msg}.", innerSignedMessage);
                    return;
                }
            }

            ctx.FireChannelRead(signedMessage);
        }

        private bool Verify(ProtocolMessage signedMessage)
        {
            if (signedMessage.Signature == null)
            {
                return false;
            }

            var sig = signedMessage.Signature.RawBytes.ToByteArray();
            var pub = signedMessage.PeerId.PublicKey.ToByteArray();
            var signature = _keySigner.CryptoContext.SignatureFromBytes(sig, pub);
            var messageWithoutSig = signedMessage.Clone();
            messageWithoutSig.Signature = null;

            return _keySigner.Verify(signature, messageWithoutSig.ToByteArray(),
                signedMessage.Signature.SigningContext);
        }
    }
}
