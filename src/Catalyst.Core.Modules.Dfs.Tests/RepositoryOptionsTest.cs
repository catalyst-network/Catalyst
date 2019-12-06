using System;
using System.IO;
using System.Linq;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.FileSystem;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests
{
    public class RepositoryOptionsTest
    {
        [Fact]
        public void Defaults()
        {
            var options = new RepositoryOptions();
            Assert.NotNull(options.Folder);
        }

        [Fact]
        public void Environment_Home()
        {
            var names = new string[] {"IPFS_PATH", "HOME", "HOMEPATH"};
            var values = names.Select(n => Environment.GetEnvironmentVariable(n));
            var sep = Path.DirectorySeparatorChar;
            try
            {
                foreach (var name in names)
                {
                    Environment.SetEnvironmentVariable(name, null);
                }

                Environment.SetEnvironmentVariable("HOME", $"{sep}home1");
                var options = new RepositoryOptions();
                Assert.Equal($"{sep}home1{sep}.csipfs", options.Folder);

                Environment.SetEnvironmentVariable("HOME", $"{sep}home2{sep}");
                options = new RepositoryOptions();
                Assert.Equal($"{sep}home2{sep}.csipfs", options.Folder);
            }
            finally
            {
                var pairs = names.Zip(values, (name, value) => new {name = name, value = value});
                foreach (var pair in pairs)
                {
                    Environment.SetEnvironmentVariable(pair.name, pair.value);
                }
            }
        }

        [Fact]
        public void Environment_HomePath()
        {
            var names = new string[] {"IPFS_PATH", "HOME", "HOMEPATH"};
            var values = names.Select(n => Environment.GetEnvironmentVariable(n));
            var sep = Path.DirectorySeparatorChar;
            try
            {
                foreach (var name in names)
                {
                    Environment.SetEnvironmentVariable(name, null);
                }

                Environment.SetEnvironmentVariable("HOMEPATH", $"{sep}home1");
                var options = new RepositoryOptions();
                Assert.Equal($"{sep}home1{sep}.csipfs", options.Folder);

                Environment.SetEnvironmentVariable("HOMEPATH", $"{sep}home2{sep}");
                options = new RepositoryOptions();
                Assert.Equal($"{sep}home2{sep}.csipfs", options.Folder);
            }
            finally
            {
                var pairs = names.Zip(values, (name, value) => new {name = name, value = value});
                foreach (var pair in pairs)
                {
                    Environment.SetEnvironmentVariable(pair.name, pair.value);
                }
            }
        }

        [Fact]
        public void Environment_IpfsPath()
        {
            var names = new string[] {"IPFS_PATH", "HOME", "HOMEPATH"};
            var values = names.Select(n => Environment.GetEnvironmentVariable(n));
            var sep = Path.DirectorySeparatorChar;
            try
            {
                foreach (var name in names)
                {
                    Environment.SetEnvironmentVariable(name, null);
                }

                Environment.SetEnvironmentVariable("IPFS_PATH", $"{sep}x1");
                var options = new RepositoryOptions();
                Assert.Equal($"{sep}x1", options.Folder);

                Environment.SetEnvironmentVariable("IPFS_PATH", $"{sep}x2{sep}");
                options = new RepositoryOptions();
                Assert.Equal($"{sep}x2{sep}", options.Folder);
            }
            finally
            {
                var pairs = names.Zip(values, (name, value) => new {name = name, value = value});
                foreach (var pair in pairs)
                {
                    Environment.SetEnvironmentVariable(pair.name, pair.value);
                }
            }
        }
    }
}
