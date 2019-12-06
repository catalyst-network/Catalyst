using System;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Options;
using MultiFormats;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class AddFileOptionsTests
    {
        [Fact]
        public void Defaults()
        {
            var options = new AddFileOptions();

            Assert.Equal(true, options.Pin);
            Assert.Equal(256 * 1024, options.ChunkSize);
            Assert.Equal(MultiHash.DefaultAlgorithmName, options.Hash);
            Assert.Equal(false, options.OnlyHash);
            Assert.Equal(false, options.RawLeaves);
            Assert.Equal(false, options.Trickle);
            Assert.Equal(false, options.Wrap);
            Assert.Null(options.Progress);
            Assert.Null(options.ProtectionKey);
        }

        [Fact]
        public void Setting()
        {
            var options = new AddFileOptions
            {
                Pin = false,
                ChunkSize = 2 * 1024,
                Hash = "sha2-512",
                OnlyHash = true,
                RawLeaves = true,
                Progress = new Progress<TransferProgress>(),
                Trickle = true,
                Wrap = true,
                ProtectionKey = "secret"
            };

            Assert.Equal(false, options.Pin);
            Assert.Equal(2 * 1024, options.ChunkSize);
            Assert.Equal("sha2-512", options.Hash);
            Assert.Equal(true, options.OnlyHash);
            Assert.Equal(true, options.RawLeaves);
            Assert.Equal(true, options.Trickle);
            Assert.Equal(true, options.Wrap);
            Assert.NotNull(options.Progress);
            Assert.Equal("secret", options.ProtectionKey);
        }
    }
}
