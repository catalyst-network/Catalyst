using System;
using BenchmarkDotNet.Attributes;
using NSec.Cryptography;
using Org.BouncyCastle.Security;

namespace Catalyst.Benchmark
{
    [CategoriesColumn]
    [BenchmarkCategory("ed25519")]
    public class Ed25519NSec
    {
        private static readonly SecureRandom Random = new SecureRandom();

        private readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;
        private byte[] _message;
        private byte[] _signature;
        private Key _key;
        
        [GlobalSetup(Target = nameof(GeneratePublicKey))]
        public void SetupGenerateKey(){
            _key = new Key(_algorithm);
        }

        [GlobalSetup(Target = nameof(Sign))]
        public void SetupSign(){
            _message = new byte[32];
            _signature = new byte[64];
            SetupGenerateKey();
            _key = Key.Create(_algorithm);
            Random.NextBytes(_message);
        }

        [GlobalSetup(Target = nameof(Verify))]
        public void SetupVerify(){
            SetupSign();
            ReadOnlySpan<byte> m = _message;
            _signature = _algorithm.Sign(_key, m); 
        }

        [Benchmark]
        [BenchmarkCategory("keygen")]
        public void GeneratePublicKey(){
            _key=Key.Create(_algorithm);
        }

        [Benchmark]
        [BenchmarkCategory("sign")]
        public void Sign()
        {
            ReadOnlySpan<byte> m = _message;
            _algorithm.Sign(_key, m); 
        }

        [Benchmark]
        [BenchmarkCategory("verify")]
        public bool Verify()
        {
            ReadOnlySpan<byte> m = _message;
            ReadOnlySpan<byte> sig = _signature;
            return _algorithm.Verify(_key.PublicKey,m,sig);
        }

    }
}
