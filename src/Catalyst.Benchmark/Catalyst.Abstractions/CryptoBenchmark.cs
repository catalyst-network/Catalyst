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
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Benchmark.Catalyst.Abstractions
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.CoreRt30)]
    public class CryptoBenchmark
    {
        public CryptoBenchmark()
        {
            _context = new SigningContext
            {
                NetworkType = NetworkType.Mainnet,
                SignatureType = SignatureType.TransactionConfidential
            };

            var key = ByteString.CopyFrom(new byte[32]);
            var amount = ByteString.CopyFrom(new byte[32]);

            _transaction = new TransactionBroadcast
            {
                PublicEntry = new PublicEntry
                {
                    Amount = amount,
                    Nonce = 1,
                    SenderAddress = key,
                    ReceiverAddress = key,
                    GasPrice = amount,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                }
            };

            _crypto = new NoopCryptoContext();
        }

        private readonly SigningContext _context;
        private readonly TransactionBroadcast _transaction;
        private readonly ICryptoContext _crypto;

        [Benchmark]
        public bool SignVerify_with_ToByteArray()
        {
            var signature = _crypto.Sign(null, _transaction.ToByteArray(), _context.ToByteArray());
            return _crypto.Verify(signature, _transaction.ToByteArray(), _context.ToByteArray());
        }

        [Benchmark]
        public bool SignVerify_with_embedded_serialization()
        {
            var signature = _crypto.Sign(null, _transaction, _context);
            return _crypto.Verify(signature, _transaction, _context);
        }

        internal class NoopCryptoContext : ICryptoContext
        {
            public ISignature Sign(IPrivateKey privateKey, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context)
            {
                return null;
            }

            public bool Verify(ISignature signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> context)
            {
                return true;
            }

            public int PrivateKeyLength => throw new NotImplementedException();
            public int PublicKeyLength => throw new NotImplementedException();
            public int SignatureLength => throw new NotImplementedException();
            public int SignatureContextMaxLength => throw new NotImplementedException();
            public IPrivateKey GeneratePrivateKey() { throw new NotImplementedException(); }

            public IPublicKey GetPublicKeyFromPrivateKey(IPrivateKey privateKey)
            {
                throw new NotImplementedException();
            }

            public IPublicKey GetPublicKeyFromBytes(byte[] publicKeyBytes) { throw new NotImplementedException(); }
            public IPrivateKey GetPrivateKeyFromBytes(byte[] privateKeyBytes) { throw new NotImplementedException(); }

            public ISignature GetSignatureFromBytes(byte[] signatureBytes, byte[] publicKeyBytes)
            {
                throw new NotImplementedException();
            }

            public byte[] ExportPrivateKey(IPrivateKey privateKey) { throw new NotImplementedException(); }
            public byte[] ExportPublicKey(IPublicKey publicKey) { throw new NotImplementedException(); }
        }
    }
}
