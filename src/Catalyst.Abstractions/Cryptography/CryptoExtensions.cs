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
using System.Buffers;
using System.IO;
using Google.Protobuf;
using MultiFormats;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using ProtoBuf;

namespace Catalyst.Abstractions.Cryptography
{
    public static class CryptoExtensions
    {
        /// <summary>
        ///     Signs message using provided private key and returns the signature.
        /// </summary>
        /// <param name="crypto"></param>
        /// <param name="privateKey"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ISignature Sign(this ICryptoContext crypto, IPrivateKey privateKey, IMessage message, IMessage context)
        {
            ProtoPreconditions.CheckNotNull(message, nameof(message));
            ProtoPreconditions.CheckNotNull(context, nameof(context));

            var messageSize = message.CalculateSize();
            var contextSize = context.CalculateSize();
            var array = ArrayPool<byte>.Shared.Rent(messageSize + contextSize);
            
            using (var output = new CodedOutputStream(array))
            {
                message.WriteTo(output);
                context.WriteTo(output);
            } 

            var signature = crypto.Sign(privateKey, array.AsSpan(0, messageSize), array.AsSpan(messageSize, contextSize));

            ArrayPool<byte>.Shared.Return(array);

            return signature;
        }

        public static bool Verify(this ICryptoContext crypto, ISignature signature, IMessage message, IMessage context)
        {
            ProtoPreconditions.CheckNotNull(message, nameof(message));
            ProtoPreconditions.CheckNotNull(context, nameof(context));

            var messageSize = message.CalculateSize();
            var contextSize = context.CalculateSize();
            var array = ArrayPool<byte>.Shared.Rent(messageSize + contextSize);

            using (var output = new CodedOutputStream(array))
            {
                message.WriteTo(output);
                context.WriteTo(output);
            }

            var result = crypto.Verify(signature, array.AsSpan(0, messageSize), array.AsSpan(messageSize, contextSize));

            ArrayPool<byte>.Shared.Return(array);

            return result;
        }
    }
}
