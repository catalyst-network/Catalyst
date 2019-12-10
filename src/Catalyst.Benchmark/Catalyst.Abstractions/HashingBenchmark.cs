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
using System.Linq;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using MultiFormats;
using MultiFormats.Registry;
using Nethermind.Core.Extensions;

namespace Catalyst.Benchmark.Catalyst.Core.Modules.Hashing
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.CoreRt30)]
    public class HashingBenchmark
    {
        public HashingBenchmark()
        {
            const string name = nameof(NoopHash);

            HashingAlgorithm.Register(name, 1234123421, NoopHash.DigestSize, () => new NoopHash());

            var bytes = ByteString.CopyFrom(Enumerable.Range(1, 32).Select(i => (byte) i).ToArray());
            var amount = ByteString.CopyFrom(343434.ToByteArray(Bytes.Endianness.Big));

            _entry = new PublicEntry
            {
                Amount = amount,
                TransactionFees = amount,
                ReceiverAddress = bytes,
                SenderAddress = bytes
            };

            _provider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata(name));
        }

        private readonly HashProvider _provider;
        private readonly PublicEntry _entry;
        private readonly byte[] _salt = {1, 2, 3, 4};

        [Benchmark]
        public MultiHash Concat_manual() { return _provider.ComputeMultiHash(_entry.ToByteArray().Concat(_salt)); }

        [Benchmark]
        public MultiHash Concat_embedded() { return _provider.ComputeMultiHash(_entry, _salt); }

        internal class NoopHash : HashAlgorithm
        {
            public const int DigestSize = 32;
            private static readonly byte[] Value = new byte[DigestSize];

            protected override void HashCore(byte[] array, int ibStart, int cbSize) { }

            protected override byte[] HashFinal() { return Value; }

            public override void Initialize() { }

            protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
            {
                bytesWritten = destination.Length;
                return true;
            }

            protected override void HashCore(ReadOnlySpan<byte> source)
            {
                // this is a noop hasher that is used only for benchmarking purposes, to shaw the overhead of non-hashing operations
            }
        }
    }
}
