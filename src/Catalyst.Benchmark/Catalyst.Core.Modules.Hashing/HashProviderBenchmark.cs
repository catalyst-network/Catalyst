using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Catalyst.Core.Modules.Hashing;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Benchmark.Catalyst.Core.Modules.Hashing
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.CoreRt30)]
    public class HashProviderBenchmark
    {
        static HashProviderBenchmark()
        {
            const string name = nameof(NoopHash);

            HashingAlgorithm.Register(name, 1234123421, NoopHash.DigestSize, () => new NoopHash());

            Provider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata(name));
        }

        static readonly HashProvider Provider;
        static readonly string Chars4 = new string('a', 4);
        static readonly string Chars32 = new string('a', 32);
        static readonly string Chars1024 = new string('a', 1024);

        [Benchmark]
        public MultiHash String_4chars()
        {
            return Provider.ComputeUtf8MultiHash(Chars4);
        }

        [Benchmark]
        public MultiHash String_32chars()
        {
            return Provider.ComputeUtf8MultiHash(Chars32);
        }

        [Benchmark]
        public MultiHash String_1024chars()
        {
            return Provider.ComputeUtf8MultiHash(Chars1024);
        }

        internal class NoopHash : HashAlgorithm
        {
            public const int DigestSize = 32;
            static readonly byte[] Value = new byte[DigestSize];

            protected override void HashCore(byte[] array, int ibStart, int cbSize) { }

            protected override byte[] HashFinal()
            {
                return Value;
            }

            public override void Initialize() { }

            protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
            {
                bytesWritten = destination.Length;
                return true;
            }

            protected override void HashCore(ReadOnlySpan<byte> source) { }
        }
    }
}
