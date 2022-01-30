#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Wire;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Lib.IO.Handlers
{
    public sealed class ProtocolMessageVerifyHandler : IInboundMessageHandler
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IKeySigner _keySigner;

        public ProtocolMessageVerifyHandler(IKeySigner keySigner)
        {
            _keySigner = keySigner;
        }

        public Task<bool> ProcessAsync(ProtocolMessage message)
        {
            Logger.Verbose("Received {msg}", message);
            if (!Verify(message))
            {
                Logger.Warning("Failed to verify {msg} signature.", message);
                return Task.FromResult(false);
            }

            if (message.IsBroadCastMessage())
            {
                var innerSignedMessage = ProtocolMessage.Parser.ParseFrom(message.Value);
                if (!Verify(innerSignedMessage))
                {
                    Logger.Warning("Failed to verify inner signature in broadcast message {msg}.", innerSignedMessage);
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }

        private bool Verify(ProtocolMessage signedMessage)
        {
            if (signedMessage.Signature == null)
            {
                return false;
            }

            var sig = signedMessage.Signature.RawBytes.ToByteArray();
            MultiAddress address = new(signedMessage.Address);
            var pub = address.GetPublicKeyBytes();

            var signature = _keySigner.CryptoContext.GetSignatureFromBytes(sig, pub);
            var messageWithoutSig = signedMessage.Clone();
            messageWithoutSig.Signature = null;

            var verified = _keySigner.Verify(signature, messageWithoutSig, signedMessage.Signature.SigningContext);

            return verified;
        }
    }
}
