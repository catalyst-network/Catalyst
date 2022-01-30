#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Org.BouncyCastle.Security;
using Secp256k1Net;

namespace Catalyst.Benchmark 
{
    [CategoriesColumn]
    [BenchmarkCategory("secp256k1")]
    public class Secp256k1Wrapped
    {
        private static readonly SecureRandom Random = new();

        private byte[] _privateKey;
        private byte[] _publicKey;
        private byte[] _message ;
        private byte[] _signature;
        readonly Secp256k1 secp256k1;
        
        public Secp256k1Wrapped()
        {
            secp256k1 = new Secp256k1();
        }

        private Span<byte> GeneratePrivateKey()
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
            var sk = GeneratePrivateKey();
            _privateKey = sk.ToArray();    
        }

        [GlobalSetup(Target = nameof(Sign))]
        public void SetupSign(){
            _signature = new byte[64];
            
            SetupGenerateKey();

            Span<byte> sk = _privateKey;

            Span<byte> pk = new byte[64];
            secp256k1.PublicKeyCreate(pk,sk);
            _publicKey=pk.ToArray();
            
            Span<byte> m = new byte[32];
            Random.NextBytes(m);
            _message = m.ToArray();
        }

        [GlobalSetup(Target = nameof(Verify))]
        public void SetupVerify(){
            SetupSign();

            Span<byte> sk = _privateKey;

            Span<byte> sig = new byte[64];
            secp256k1.Sign(sig, _message, sk);
            _signature = sig.ToArray();
        }

        [Benchmark]
        [BenchmarkCategory("keygen")]
        public void GeneratePublicKey(){
            Span<byte> sk = _privateKey;

            Span<byte> pk = new byte[64];
            secp256k1.PublicKeyCreate(pk,sk);  
        }
        
        [Benchmark]
        [BenchmarkCategory("sign")]
        public void Sign()
        {
            Span<byte> m = _message;

            Span<byte> sk = _privateKey;

            Span<byte> sig = new byte[64];
            secp256k1.Sign(sig, m, sk);   
        }

        [Benchmark]
        [BenchmarkCategory("verify")]
        public bool Verify()
        {
            Span<byte> sig = _signature;

            Span<byte> m = _message;

            Span<byte> pk = _publicKey;

            return secp256k1.Verify(sig,m,pk);
        }
    }
}
