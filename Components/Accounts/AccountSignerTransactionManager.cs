﻿using System;
using System.Numerics;
using System.Threading.Tasks;
using ADL.Hex.HexConvertors.Extensions;
using ADL.Hex.HexTypes;
//using ADL.JsonRpc.Client;
using ADL.KeyStore;
//using ADL.RPC.Accounts;
//using ADL.RPC.Eth.DTOs;
//using ADL.RPC.Eth.Transactions;
//using ADL.RPC.NonceServices;
//using ADL.RPC.TransactionManagers;
using ADL.KeySigner;

//using Transaction = ADL.Signer.Transaction;

namespace ADL.Accounts
{


    public class AccountSignerTransactionManager : TransactionManagerBase
    {
        private readonly TransactionSigner _transactionSigner;
        public BigInteger? ChainId { get; private set; }

        public AccountSignerTransactionManager(IClient rpcClient, Account account, BigInteger? chainId = null)
        {
            ChainId = chainId;
            Account = account ?? throw new ArgumentNullException(nameof(account));
            Client = rpcClient;
            _transactionSigner = new TransactionSigner();
        }


        public AccountSignerTransactionManager(IClient rpcClient, string privateKey, BigInteger? chainId = null)
        {
            ChainId = chainId;
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));
            Client = rpcClient;
            Account = new Account(privateKey);
            Account.NonceService = new InMemoryNonceService(Account.Address, rpcClient);
            _transactionSigner = new TransactionSigner();
        }

        public AccountSignerTransactionManager(string privateKey, BigInteger? chainId = null) : this(null, privateKey, chainId)
        {

        }

        public override BigInteger DefaultGas { get; set; } = Transaction.DEFAULT_GAS_LIMIT;


        public override Task<string> SendTransactionAsync(TransactionInput transactionInput)
        {
            if (transactionInput == null) throw new ArgumentNullException(nameof(transactionInput));
            return SignAndSendTransactionAsync(transactionInput);
        }

        public override Task<string> SignTransactionAsync(TransactionInput transaction)
        {
            return SignTransactionRetrievingNextNonceAsync(transaction);
        }

        public string SignTransaction(TransactionInput transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (!string.Equals(transaction.From.EnsureHexPrefix(), Account.Address.EnsureHexPrefix(), StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("Invalid account used signing");
            SetDefaultGasPriceAndCostIfNotSet(transaction);

            var nonce = transaction.Nonce;
            if(nonce == null) throw new ArgumentNullException(nameof(transaction), "Transaction nonce has not been set");

            var gasPrice = transaction.GasPrice;
            var gasLimit = transaction.Gas;

            var value = transaction.Value ?? new HexBigInteger(0);

            string signedTransaction;

            if (ChainId == null)
            {
                signedTransaction = _transactionSigner.SignTransaction(((Account)Account).PrivateKey,
                    transaction.To,
                    value.Value, nonce,
                    gasPrice.Value, gasLimit.Value, transaction.Data);
            }
            else
            {
                signedTransaction = _transactionSigner.SignTransaction(((Account)Account).PrivateKey, ChainId.Value,
                    transaction.To,
                    value.Value, nonce,
                    gasPrice.Value, gasLimit.Value, transaction.Data);
            }

            return signedTransaction;
        }

        protected async Task<string> SignTransactionRetrievingNextNonceAsync(TransactionInput transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (transaction.From.EnsureHexPrefix().ToLower() != Account.Address.EnsureHexPrefix().ToLower())
                throw new Exception("Invalid account used signing");
            var nonce = await GetNonceAsync(transaction).ConfigureAwait(false);
            transaction.Nonce = nonce;
            var gasPrice = await GetGasPriceAsync(transaction).ConfigureAwait(false);
            transaction.GasPrice = gasPrice;
            return SignTransaction(transaction);
        }

        public async Task<HexBigInteger> GetNonceAsync(TransactionInput transaction)
        {
            if (Client == null) throw new NullReferenceException("Client not configured");
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            var nonce = transaction.Nonce;
            if (nonce == null)
            {
                if (Account.NonceService == null)
                    Account.NonceService = new InMemoryNonceService(Account.Address, Client);
                Account.NonceService.Client = Client;
                nonce = await Account.NonceService.GetNextNonceAsync().ConfigureAwait(false);
            }
            return nonce;
        }

        private async Task<string> SignAndSendTransactionAsync(TransactionInput transaction)
        {
            if (Client == null) throw new NullReferenceException("Client not configured");
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (transaction.From.EnsureHexPrefix().ToLower() != Account.Address.EnsureHexPrefix().ToLower())
                throw new Exception("Invalid account used signing");

            var ethSendTransaction = new EthSendRawTransaction(Client);
            var signedTransaction = await SignTransactionRetrievingNextNonceAsync(transaction).ConfigureAwait(false);
            return await ethSendTransaction.SendRequestAsync(signedTransaction.EnsureHexPrefix()).ConfigureAwait(false);
        }
    }
}