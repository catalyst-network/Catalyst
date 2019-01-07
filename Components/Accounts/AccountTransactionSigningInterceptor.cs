﻿using System;
using System.Threading.Tasks;
using ADL.JsonRpc.Client;
using ADL.RPC.Eth.DTOs;

namespace ADL.Accounts
{
    public class AccountTransactionSigningInterceptor : RequestInterceptor
    {
        private readonly AccountSignerTransactionManager signer;

        public AccountTransactionSigningInterceptor(string privateKey, IClient client)
        {
            signer = new AccountSignerTransactionManager(client, privateKey);
        }

        public override async Task<object> InterceptSendRequestAsync<TResponse>(
            Func<RpcRequest, string, Task<TResponse>> interceptedSendRequestAsync, RpcRequest request,
            string route = null)
        {
            if (request.Method == "eth_sendTransaction")
            {
                var transaction = (TransactionInput) request.RawParameters[0];
                return await SignAndSendTransaction(transaction).ConfigureAwait(false);
            }
            return await base.InterceptSendRequestAsync(interceptedSendRequestAsync, request, route)
                .ConfigureAwait(false);
        }

        public override async Task<object> InterceptSendRequestAsync<T>(
            Func<string, string, object[], Task<T>> interceptedSendRequestAsync, string method,
            string route = null, params object[] paramList)
        {
            if (method == "eth_sendTransaction")
            {
                var transaction = (TransactionInput) paramList[0];
                return await SignAndSendTransaction(transaction).ConfigureAwait(false);
            }
            return await base.InterceptSendRequestAsync(interceptedSendRequestAsync, method, route, paramList)
                .ConfigureAwait(false);
        }

        private async Task<string> SignAndSendTransaction(TransactionInput transaction)
        {
            return await signer.SendTransactionAsync(transaction).ConfigureAwait(false);
        }
    }
}