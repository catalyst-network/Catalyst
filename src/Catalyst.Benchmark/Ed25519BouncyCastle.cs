using BenchmarkDotNet.Attributes;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Security;

namespace Catalyst.Benchmark
{
    [CategoriesColumn]
    [BenchmarkCategory("ed25519")]
    public class Ed25519BouncyCastle
    {
        private static readonly SecureRandom Random = new SecureRandom();
        byte[] privateKey;
        byte[] publicKey;
        byte[] message;
        byte[] signature;
        int messageLength = 32;
        
        public Ed25519BouncyCastle()
        {
            Ed25519.Precompute();
        }

        [GlobalSetup(Target = nameof(GeneratePublicKey))]
        public void SetupGenerateKey(){
            privateKey = new byte[Ed25519.SecretKeySize];
            publicKey = new byte[Ed25519.PublicKeySize];
            Random.NextBytes(privateKey);
        }

        [GlobalSetup(Target = nameof(Sign))]
        public void SetupSign(){
            message = new byte[32];
            signature = new byte[Ed25519.SignatureSize];
            
            SetupGenerateKey();
            
            Ed25519.GeneratePublicKey(privateKey, 0, publicKey, 0);
            Random.NextBytes(message);
        }

        [GlobalSetup(Target = nameof(Verify))]
        public void SetupVerify(){
            SetupSign();
            Ed25519.Sign(privateKey, 0, message, 0, messageLength, signature, 0);

        }

		[Benchmark]
        [BenchmarkCategory("keygen")]
        public void GeneratePublicKey()
        {
            Ed25519.GeneratePublicKey(privateKey, 0, publicKey, 0);
        }

        [Benchmark]
        [BenchmarkCategory("sign")]
        public void Sign()
        {
            Ed25519.Sign(privateKey, 0, message, 0, messageLength, signature, 0);
        }

        [Benchmark]
        [BenchmarkCategory("verify")]
        public bool Verify()
        {   
            return Ed25519.Verify(signature, 0, publicKey, 0, message, 0, messageLength);
        }
    }
}
