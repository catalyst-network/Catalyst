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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Lib.Extensions.Protocol.Wire
{
    public static class TransactionBroadcastExtensions
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        public static TransactionBroadcast Sign(this TransactionBroadcast transaction,
            ICryptoContext cryptoContext,
            IPrivateKey privateKey,
            SigningContext context)
        {
            var clone = transaction.Clone();

            if (transaction.PublicEntry.Signature?.RawBytes.Length == cryptoContext.SignatureLength)
            {
                Logger.Debug("The transaction was already signed, returning a clone.");
                return clone;
            }

            clone.PublicEntry.Signature = null;
            var signatureBytes = cryptoContext.Sign(privateKey, clone.ToByteArray(),
                context.ToByteArray()).SignatureBytes;

            clone.PublicEntry.Signature = new Signature
            {
                RawBytes = signatureBytes.ToByteString(),
                SigningContext = context
            };

            return clone;
        }

        public static Signature GenerateSignature(this PublicEntry publicEntry,
            ICryptoContext cryptoContext,
            IPrivateKey privateKey,
            SigningContext context)
        {
            var clone = publicEntry.Clone();
            clone.Signature = null;
            var signatureBytes = cryptoContext.Sign(privateKey, clone.ToByteArray(),
                context.ToByteArray()).SignatureBytes;
            return new Signature
            {
                RawBytes = signatureBytes.ToByteString(),
                SigningContext = context
            };
        }

        public static PublicEntry Sign(this PublicEntry publicEntry,
            ICryptoContext cryptoContext,
            IPrivateKey privateKey,
            SigningContext context)
        {
            var clone = publicEntry.Clone();

            if (publicEntry.Signature?.RawBytes.Length == cryptoContext.SignatureLength)
            {
                Logger.Debug("The transaction was already signed, returning a clone.");
                return publicEntry;
            }

            clone.Signature = null;
            var signatureBytes = cryptoContext.Sign(privateKey, clone.ToByteArray(),
                context.ToByteArray()).SignatureBytes;

            clone.Signature = new Signature
            {
                RawBytes = signatureBytes.ToByteString(),
                SigningContext = context
            };

            return clone;
        }
    }
}
