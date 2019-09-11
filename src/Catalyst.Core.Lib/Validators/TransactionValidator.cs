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

using System.Linq;
using Catalyst.Abstractions.Validators;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Lib.Validators
{
    public class TransactionValidator : ITransactionValidator
    {
        private readonly ILogger _logger;
        private readonly IWrapper _cryptoContext;

        public TransactionValidator(ILogger logger,
            IWrapper cryptoContext)
        {
            _cryptoContext = cryptoContext;
            _logger = logger;
        }

        public bool ValidateTransaction(TransactionBroadcast transactionBroadcast, Protocol.Common.Network network)
        {
            return ValidateTransactionFields(transactionBroadcast)
             && CheckContractInputFields(transactionBroadcast)
             && CheckCfEntries(transactionBroadcast)
             && CheckStEntries(transactionBroadcast)
             && ValidateTransactionSignature(transactionBroadcast, network);
        }

        private bool CheckContractInputFields(TransactionBroadcast transactionBroadcast)
        {
            if (transactionBroadcast.Data != ByteString.Empty && transactionBroadcast.Init != ByteString.Empty)
            {
                _logger.Error("Cannot deploy smart contract & contain call data.");
                return false;
            }

            if (transactionBroadcast.Init != ByteString.Empty
             && (transactionBroadcast.STEntries.Any() || transactionBroadcast.CFEntries.Any()))
            {
                _logger.Error("Contract deployment cannot contain any entries.");
                return false;
            }

            return true;
        }

        private bool ValidateTransactionFields(TransactionBroadcast transactionBroadcast)
        {
            if (!CheckKeySize(transactionBroadcast.From))
            {
                _logger.Error($"Invalid public key on field {nameof(transactionBroadcast.From)}");
                return false;
            }

            if (transactionBroadcast.TimeStamp == null)
            {
                _logger.Error("Transaction timestamp is null");
                return false;
            }

            var isEmptyPublicTransaction = transactionBroadcast.TransactionType == TransactionType.Normal &&
                transactionBroadcast.STEntries.Count == 0;
            var isEmptyConfidentialTransaction = transactionBroadcast.TransactionType == TransactionType.Confidential &&
                transactionBroadcast.CFEntries.Count == 0;
            var isEmptySmartContractDeployment = transactionBroadcast.Init == ByteString.Empty;

            if (isEmptySmartContractDeployment 
             && (isEmptyPublicTransaction || isEmptyConfidentialTransaction))
            {
                _logger.Error("No Entries exist in the transaction");
                return false;
            }

            if (transactionBroadcast.TransactionType == TransactionType.Normal && transactionBroadcast.CFEntries.Any())
            {
                _logger.Information($"Normal transactions cannot contain any {nameof(transactionBroadcast.CFEntries)}");
                return false;
            }

            if (transactionBroadcast.TransactionType == TransactionType.Confidential &&
                transactionBroadcast.STEntries.Any())
            {
                _logger.Information($"Normal transactions cannot contain any {nameof(transactionBroadcast.STEntries)}");
                return false;
            }

            return true;
        }

        private bool ValidateTransactionSignature(TransactionBroadcast transactionBroadcast, Protocol.Common.Network network)
        {
            if (transactionBroadcast.Signature == ByteString.Empty)
            {
                _logger.Error("Transaction signature is null");
                return false;
            }

            var transactionSignature = _cryptoContext.SignatureFromBytes(transactionBroadcast.Signature.ToByteArray(), 
                transactionBroadcast.From.ToByteArray());
            var transactionWithoutSig = transactionBroadcast.Clone();
            transactionWithoutSig.Signature = ByteString.Empty;

            var signingContext = new SigningContext
            {
                SignatureType = transactionBroadcast.TransactionType == TransactionType.Normal 
                    ? SignatureType.TransactionPublic 
                    : SignatureType.TransactionConfidential,
                Network = network
            };

            if (!_cryptoContext.StdVerify(transactionSignature, transactionWithoutSig.ToByteArray(), signingContext.ToByteArray()))
            {
                _logger.Information(
                    "Transaction Signature {signature} invalid.",
                    transactionSignature);
                return false;
            }

            return true;
        }

        private bool CheckCfEntries(TransactionBroadcast transactionBroadcast)
        {
            if (transactionBroadcast.CFEntries.Count <= 0)
            {
                return true;
            }

            foreach (var cfTransactionEntry in transactionBroadcast.CFEntries)
            {
                if (!CheckKeySize(cfTransactionEntry.PubKey))
                {
                    _logger.Error($"Invalid public key on field {nameof(cfTransactionEntry.PubKey)}");
                    return false;
                }

                if (cfTransactionEntry.PedersenCommit == ByteString.Empty)
                {
                    _logger.Error($"Invalid field {nameof(cfTransactionEntry.PedersenCommit)}");
                    return false;
                }
            }

            return true;
        }

        private bool CheckStEntries(TransactionBroadcast transactionBroadcast)
        {
            if (transactionBroadcast.STEntries.Count <= 0)
            {
                return true;
            }

            foreach (var stTransactionEntry in transactionBroadcast.STEntries)
            {
                if (!CheckKeySize(stTransactionEntry.PubKey))
                {
                    _logger.Error($"Invalid public key on field {nameof(stTransactionEntry.PubKey)}");
                    return false;
                }

                if (transactionBroadcast.Data == ByteString.Empty && stTransactionEntry.Amount <= 0)
                {
                    _logger.Error(
                        $"Invalid amount on field {nameof(stTransactionEntry.Amount)}, Amount: {stTransactionEntry.Amount}");
                    return false;
                }
            }

            return true;
        }

        private bool CheckKeySize(ByteString publicKey)
        {
            return publicKey.Length == _cryptoContext.PublicKeyLength;
        }
    }
}
