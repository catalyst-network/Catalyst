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
        private byte[] _privateKey;
        private byte[] _publicKey;
        private byte[] _message;
        private byte[] _signature;
        private const int MessageLength = 32;

        public Ed25519BouncyCastle()
        {
            Ed25519.Precompute();
        }

        [GlobalSetup(Target = nameof(GeneratePublicKey))]
        public void SetupGenerateKey(){
            _privateKey = new byte[Ed25519.SecretKeySize];
            _publicKey = new byte[Ed25519.PublicKeySize];
            Random.NextBytes(_privateKey);
        }

        [GlobalSetup(Target = nameof(Sign))]
        public void SetupSign(){
            _message = new byte[MessageLength];
            _signature = new byte[Ed25519.SignatureSize];
            
            SetupGenerateKey();
            
            Ed25519.GeneratePublicKey(_privateKey, 0, _publicKey, 0);
            Random.NextBytes(_message);
        }

        [GlobalSetup(Target = nameof(Verify))]
        public void SetupVerify(){
            SetupSign();
            Ed25519.Sign(_privateKey, 0, _message, 0, MessageLength, _signature, 0);

        }

		[Benchmark]
        [BenchmarkCategory("keygen")]
        public void GeneratePublicKey()
        {
            Ed25519.GeneratePublicKey(_privateKey, 0, _publicKey, 0);
        }

        [Benchmark]
        [BenchmarkCategory("sign")]
        public void Sign()
        {
            Ed25519.Sign(_privateKey, 0, _message, 0, MessageLength, _signature, 0);
        }

        [Benchmark]
        [BenchmarkCategory("verify")]
        public bool Verify()
        {   
            return Ed25519.Verify(_signature, 0, _publicKey, 0, _message, 0, MessageLength);
        }
    }
}
