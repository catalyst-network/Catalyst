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

using System.Numerics;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class TransactionForRpc
    {
        public TransactionForRpc(Keccak blockHash, BigInteger? blockNumber, int? txIndex, Transaction transaction)
        {
            Hash = transaction.Hash;
            Nonce = transaction.Nonce;
            BlockHash = blockHash;
            BlockNumber = blockNumber;
            TransactionIndex = txIndex;
            From = transaction.SenderAddress;
            To = transaction.To;
            Value = transaction.Value;
            GasPrice = transaction.GasPrice;
            Gas = transaction.GasLimit;
            Input = Data = transaction.Data ?? transaction.Init;
            R = transaction.Signature?.R;
            S = transaction.Signature?.S;
            V = (UInt256?) transaction.Signature?.V;
        }

        // ReSharper disable once UnusedMember.Global
        public TransactionForRpc() { }

        public Keccak Hash { get; set; }
        public BigInteger? Nonce { get; set; }
        public Keccak BlockHash { get; set; }
        public BigInteger? BlockNumber { get; set; }
        public BigInteger? TransactionIndex { get; set; }
        public Address From { get; set; }
        public Address To { get; set; }
        public BigInteger? Value { get; set; }
        public BigInteger? GasPrice { get; set; }
        public BigInteger? Gas { get; set; }
        public byte[] Data { get; set; }
        public byte[] Input { get; set; }
        public UInt256? V { get; set; }

        public byte[] S { get; set; }

        public byte[] R { get; set; }

        public Transaction ToTransaction()
        {
            Transaction tx = new Transaction();
            tx.GasLimit = (long) (Gas ?? 90000);
            tx.GasPrice = (UInt256) (GasPrice ?? 20.GWei());
            tx.Nonce = (ulong) (Nonce ?? 0); // here pick the last nonce?
            tx.To = To;
            tx.SenderAddress = From;
            tx.Value = (UInt256) (Value ?? 0);
            if (tx.To == null)
            {
                tx.Init = Data ?? Input;
            }
            else
            {
                tx.Data = Data ?? Input;
            }

            return tx;
        }
    }
}
