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

namespace Catalyst.Benchmark.Catalyst.Core.Modules.Hashing
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.CoreRt30)]
    public class CryptoBenchmark
    {
        static CryptoBenchmark()
        {
            Context = new SigningContext
            {
                NetworkType = NetworkType.Mainnet,
                SignatureType = SignatureType.TransactionConfidential
            };

            var key = ByteString.CopyFrom(new byte[32]);
            var amount = ByteString.CopyFrom(new byte[32]);

            Transaction = new TransactionBroadcast
            {
                PublicEntries =
                {
                    new PublicEntry
                    {
                        Amount = amount,
                        Base = new BaseEntry
                        {
                            Nonce = 1,
                            SenderPublicKey = key,
                            ReceiverPublicKey = key,
                            TransactionFees = amount
                        }
                    }
                },
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            };

            Crypto = new NoopCryptoContext();
        }

        static readonly SigningContext Context;
        static readonly TransactionBroadcast Transaction;
        static readonly ICryptoContext Crypto;
        static readonly IPrivateKey PrivateKey = null;

        [Benchmark]
        public bool SignVerify_with_ToByteArray()
        {
            var signature = Crypto.Sign(PrivateKey, Transaction.ToByteArray(), Context.ToByteArray());
            return Crypto.Verify(signature, Transaction.ToByteArray(), Context.ToByteArray());
        }
        
        [Benchmark]
        public bool SignVerify_with_embedded_serialization()
        {
            var signature = Crypto.Sign(PrivateKey, Transaction, Context);
            return Crypto.Verify(signature, Transaction, Context);
        }

        internal class NoopCryptoContext : ICryptoContext
        {
            public ISignature Sign(IPrivateKey privateKey, byte[] message, byte[] context) => null;
            public ISignature Sign(IPrivateKey privateKey, byte[] message, int messageLength, byte[] context, int contextLength) => null;
            public bool Verify(ISignature signature, byte[] message, byte[] context) => true;
            public bool Verify(ISignature signature, byte[] message, int messageLength, byte[] context, int contextLength) => true;

            public int PrivateKeyLength => throw new NotImplementedException();
            public int PublicKeyLength => throw new NotImplementedException();
            public int SignatureLength => throw new NotImplementedException();
            public int SignatureContextMaxLength => throw new NotImplementedException();
            public IPrivateKey GeneratePrivateKey() { throw new NotImplementedException(); }
            public IPublicKey GetPublicKeyFromPrivateKey(IPrivateKey privateKey) { throw new NotImplementedException(); }
            public IPublicKey GetPublicKeyFromBytes(byte[] publicKeyBytes) { throw new NotImplementedException(); }
            public IPrivateKey GetPrivateKeyFromBytes(byte[] privateKeyBytes) { throw new NotImplementedException(); }
            public ISignature GetSignatureFromBytes(byte[] signatureBytes, byte[] publicKeyBytes) { throw new NotImplementedException(); }
            public byte[] ExportPrivateKey(IPrivateKey privateKey) { throw new NotImplementedException(); }
            public byte[] ExportPublicKey(IPublicKey publicKey) { throw new NotImplementedException(); }
        }
    }
}
