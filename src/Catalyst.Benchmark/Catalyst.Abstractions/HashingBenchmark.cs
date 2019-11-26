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
using Nethermind.Core.Extensions;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Benchmark.Catalyst.Core.Modules.Hashing
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.CoreRt30)]
    public class HashingBenchmark
    {
        static HashingBenchmark()
        {
            const string name = nameof(NoopHash);

            HashingAlgorithm.Register(name, 1234123421, NoopHash.DigestSize, () => new NoopHash());

            var bytes = ByteString.CopyFrom(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());
            var amount = ByteString.CopyFrom(343434.ToByteArray(Bytes.Endianness.Big));

            Entry = new PublicEntry
            {
                Amount = amount,
                Base = new BaseEntry
                {
                    TransactionFees = amount,
                    ReceiverPublicKey = bytes,
                    SenderPublicKey = bytes,
                }
            };

            Provider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata(name));
        }

        static readonly HashProvider Provider;
        static readonly PublicEntry Entry;
        static readonly byte[] Salt = {1, 2, 3, 4};

        [Benchmark]
        public MultiHash Concat_manual()
        {
            return Provider.ComputeMultiHash(Entry.ToByteArray().Concat(Salt));
        }

        [Benchmark]
        public MultiHash Concat_embedded()
        {
            return Provider.ComputeMultiHash(Entry, Salt);
        }

        internal class NoopHash : HashAlgorithm
        {
            public const int DigestSize = 32;
            static readonly byte[] Value = new byte[DigestSize];

            protected override void HashCore(byte[] array, int ibStart, int cbSize) { }

            protected override byte[] HashFinal() => Value;

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
