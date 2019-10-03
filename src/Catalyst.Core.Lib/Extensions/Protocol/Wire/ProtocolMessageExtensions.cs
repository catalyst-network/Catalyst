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
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Lib.Extensions.Protocol.Wire
{
    public static class ProtocolMessageExtensions
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Use this to sign a <see cref="ProtocolMessage"/> before sending it.
        /// If a signature was found on the message, this will error.
        /// </summary>
        /// <param name="protocolMessage">The message that needs to be signed.</param>
        /// <param name="keySigner">An instance of <see cref="IKeySigner"/> used to build the signature.</param>
        /// <param name="signingContext">The context used to sign the message.</param>
        public static ProtocolMessage Sign(this ProtocolMessage protocolMessage,
            IKeySigner keySigner,
            SigningContext signingContext)
        {
            if ((protocolMessage.Signature?.RawBytes.Length ?? 0) == keySigner.CryptoContext.SignatureLength)
            {
                Logger.Debug("The protocol message was already signed, returning a clone.");
                return protocolMessage.Clone();
            }

            protocolMessage.Signature = null;
            var signatureBytes = keySigner.Sign(protocolMessage.ToByteArray(),
                signingContext).SignatureBytes;
            var signature = new Signature
            {
                SigningContext = signingContext,
                RawBytes = signatureBytes.ToByteString()
            };
            protocolMessage.Signature = signature;
            return protocolMessage;
        }
    }
}
