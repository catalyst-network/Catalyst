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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Benchmark
{
    [CategoriesColumn]
    [BenchmarkCategory("ed25519", "ed25519ph")]
    public class Ed25519phCatalystFfi
    {
        private readonly ICryptoContext _cryptoContext = new FfiWrapper();
        private static readonly Random Random = new Random();
        private IPrivateKey _privateKey;
        private byte[] _message;
        private byte[] _context;
        private ISignature _signature;
        private IList<ISignature> _signatures;
        private List<byte[]> _messages;

        [field: Params(1, 10, 100, 1000, 10000, 100000)]
        public int N { get; set; }


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
            for (var i = 0; i < N; i++)
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
            _cryptoContext.GetPublicKeyFromPrivateKey(_privateKey);
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
