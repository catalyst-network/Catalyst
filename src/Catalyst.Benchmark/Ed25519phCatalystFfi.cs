using System;
using BenchmarkDotNet.Attributes;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using System.Collections.Generic;
using System.Text;

namespace CryptoBenchmarks
{
    [CategoriesColumn]
    [BenchmarkCategory("ed25519", "ed25519ph")]
    public class Ed25519phCatalystFfi
    {
        ICryptoContext _cryptoContext = new FfiWrapper();
        private static readonly Random Random = new Random();
        IPrivateKey _privateKey;
        private IPublicKey _publicKey;
        byte[] _message;
        byte[] _context;
        ISignature _signature;
        IList<ISignature> _signatures;
        private List<byte[]> _messages;

        [Params(1, 10, 100, 1000, 10000, 100000)]
        public int N;

        [GlobalSetup(Target = nameof(GetPublicKey))]
        public void SetupGetPublicKey()
        {
            _privateKey = _cryptoContext.GeneratePrivateKey();
        }

        [GlobalSetup(Target = nameof(Sign))]
        public void SetupSign()
        {
            _message = new byte[32];
            _context = new byte[32];
            SetupGetPublicKey();
        }

        [GlobalSetup(Target = nameof(Verify))]
        public void SetupVerify()
        {
            SetupSign();
            _signature = _cryptoContext.Sign(_privateKey, _message, _context);
        }
        [GlobalSetup(Target = nameof(BatchVerify))]
        public void SetupBatchVerify()
        {
            _messages = new List<byte[]>();
            _signatures = new List<ISignature>();
            _context = Encoding.UTF8.GetBytes("context");
            for (int i = 0; i < N; i++)
            {
                var bytes = new byte[255];
                Random.NextBytes(bytes);
                _messages.Add(bytes);
            }
            _messages.ForEach(x =>
            {
                _signatures.Add(_cryptoContext.Sign(_cryptoContext.GeneratePrivateKey(), x, _context));
            });
        }

        [Benchmark]
        [BenchmarkCategory("keygen")]
        public void GeneratePrivateKey()
        {
            _privateKey = _cryptoContext.GeneratePrivateKey();

        }

        [Benchmark]
        [BenchmarkCategory("getpublickey")]
        public void GetPublicKey()
        {
            _publicKey = _cryptoContext.GetPublicKeyFromPrivateKey(_privateKey);
        }

  

        [Benchmark]
        [BenchmarkCategory("sign")]
        public void Sign()
        {
            _signature = _cryptoContext.Sign(_privateKey, _message, _context);
        }

        [Benchmark]
        [BenchmarkCategory("verify")]
        public bool Verify()
        {
            return _cryptoContext.Verify(_signature, _message, _context);
        }

        [Benchmark]
        [BenchmarkCategory("batchverify", "verify")]
        public bool BatchVerify()
        {
            return _cryptoContext.BatchVerify(_signatures, _messages, _context);
        }

    }
}