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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Validators;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Lib.Validators
{
    public sealed class TransactionValidator : ITransactionValidator
    {
        private readonly ILogger _logger;
        private readonly ICryptoContext _cryptoContext;

        public TransactionValidator(ILogger logger,
            ICryptoContext cryptoContext)
        {
            _cryptoContext = cryptoContext;
            _logger = logger;
        }

        public bool ValidateTransaction(PublicEntry transaction)
        {
            // will add more checks
            return ValidateTransactionSignature(transaction);
        }
        
        private bool ValidateTransactionSignature(PublicEntry transaction)
        {
            if (transaction.Signature.RawBytes == ByteString.Empty)
            {
                _logger.Error("Transaction signature is null");
                return false;
            }

            var transactionSignature = _cryptoContext.GetSignatureFromBytes(transaction.Signature.RawBytes.ToByteArray(),
                transaction.Base.SenderPublicKey.ToByteArray());

            var signingContext = transaction.Signature.SigningContext.ToByteArray();

            // we need to verify the signature matches the message, but transactionBroadcast contains the signature and original data,
            // passing message+sig will mean your verifying an incorrect message and always return false, so just null the sig.
            var transactionClone = transaction.Clone();
            transactionClone.Signature = null;

            if (_cryptoContext.Verify(transactionSignature, transactionClone.ToByteArray(), signingContext))
            {
                return true;
            }
            
            _logger.Information(
                "Transaction Signature {signature} invalid.",
                transactionSignature);
            return false;
        }
    }
}
