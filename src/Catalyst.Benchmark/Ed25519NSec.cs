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
        
        SignatureAlgorithm algorithm = SignatureAlgorithm.Ed25519;
        byte[] message;
        byte[] signature;
        Key key;
        
        [GlobalSetup(Target = nameof(GeneratePublicKey))]
        public void SetupGenerateKey(){
            key = new Key(algorithm);
        }

        [GlobalSetup(Target = nameof(Sign))]
        public void SetupSign(){
            message = new byte[32];
            signature = new byte[64];
            SetupGenerateKey();
            key = Key.Create(algorithm);
            Random.NextBytes(message);
        }

        [GlobalSetup(Target = nameof(Verify))]
        public void SetupVerify(){
            SetupSign();
            ReadOnlySpan<byte> m = message;
            signature = algorithm.Sign(key, m); 
        }

        [Benchmark]
        [BenchmarkCategory("keygen")]
        public void GeneratePublicKey(){
            key=Key.Create(algorithm);
        }

        [Benchmark]
        [BenchmarkCategory("sign")]
        public void Sign()
        {
            ReadOnlySpan<byte> m = message;
            algorithm.Sign(key, m); 
        }

        [Benchmark]
        [BenchmarkCategory("verify")]
        public bool Verify()
        {
            ReadOnlySpan<byte> m = message;
            ReadOnlySpan<byte> sig = signature;
            return algorithm.Verify(key.PublicKey,m,sig);
        }

    }
}
