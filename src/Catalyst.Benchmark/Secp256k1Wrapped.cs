using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Org.BouncyCastle.Security;
using Secp256k1Net;

namespace Catalyst.Benchmark 
{
    [CategoriesColumn]
    [BenchmarkCategory("secp256k1")]
    public class Secp256k1Wrapped
    {
        private static readonly SecureRandom Random = new SecureRandom();
        
        byte[] privateKey;
        byte[] publicKey;
        byte[] message ;
        byte[] signature;
        Secp256k1 secp256k1;
        
        public Secp256k1Wrapped()
        {
            secp256k1 = new Secp256k1();
        }

        Span<byte> GeneratePrivateKey()
        {
            Span<byte> sk = new byte[32];
            do
            {
                Random.NextBytes(sk);
            }
            while (!secp256k1.SecretKeyVerify(sk));
            return sk;
        }


        [GlobalSetup(Target = nameof(GeneratePublicKey))]
        public void SetupGenerateKey(){
            Span<byte> sk = GeneratePrivateKey();
            privateKey = sk.ToArray();    
        }

        [GlobalSetup(Target = nameof(Sign))]
        public void SetupSign(){
            signature = new byte[64];
            
            SetupGenerateKey();
            
            Span<byte> sk = new byte[64];
            sk = privateKey;

            Span<byte> pk = new byte[64];
            secp256k1.PublicKeyCreate(pk,sk);
            publicKey=pk.ToArray();
            
            Span<byte> m = new byte[32];
            Random.NextBytes(m);
            message = m.ToArray();
        }

        [GlobalSetup(Target = nameof(Verify))]
        public void SetupVerify(){
            SetupSign();
            
            Span<byte> sk = new byte[64];
            sk = privateKey;

            Span<byte> m = new byte[32];
            m = message;

            Span<byte> sig = new byte[64];
            secp256k1.Sign(sig, message, sk);
            signature = sig.ToArray();
        }

        [Benchmark]
        [BenchmarkCategory("keygen")]
        public void GeneratePublicKey(){
            Span<byte> sk = new byte[64];
            sk = privateKey;

            Span<byte> pk = new byte[64];
            secp256k1.PublicKeyCreate(pk,sk);  
        }
        [Benchmark]
        [BenchmarkCategory("sign")]
        public void Sign()
        {
            Span<byte> m = new byte[32];
            m = message;

            Span<byte> sk = new byte[64];
            sk=privateKey;

            Span<byte> sig = new byte[64];
            secp256k1.Sign(sig, m, sk);   
        }

        [Benchmark]
        [BenchmarkCategory("verify")]
        public bool Verify()
        {
            Span<byte> sig = new byte[64];
            sig=signature;

            Span<byte> m = new byte[32];
            m = message;

            Span<byte> pk = new byte[64];
            pk= publicKey;

            return secp256k1.Verify(sig,m,pk);
    
        }

    }
}