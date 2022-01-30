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
using NSec.Cryptography;
using Org.BouncyCastle.Security;

namespace Catalyst.Benchmark
{
    [CategoriesColumn]
    [BenchmarkCategory("ed25519")]
    public class Ed25519NSec
    {
        private static readonly SecureRandom Random = new();

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
