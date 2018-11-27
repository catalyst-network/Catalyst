using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Transactions;
using ADL.Math;

namespace ADL.Wallet
{
    public abstract class Wallet : IDisposable
    {
        private static readonly Random rand = new Random();
        
        public abstract string  Name { get; }
        
        public abstract Version Version { get; }
        
        public abstract WalletAccount CreateAccount(byte[] privateKey);
        
        public abstract bool DeleteAccount(byte[] privateKey);
        
        public abstract WalletAccount GetAccount(byte[] privateKey);
        
        public abstract bool    Contains(byte[] privateKey);
        
        public abstract IEnumerable<WalletAccount> GetAccounts();
        
//        public abstract IEnumerable<Coin> GetCoins(IEnumerable<UInt160> accounts);
        
        public abstract IEnumerable<UInt256> GetTransactions();

        public WalletAccount CreateAccount()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            WalletAccount account = CreateAccount(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return account;
        }

        public virtual void Dispose()
        {
        }
    }
}