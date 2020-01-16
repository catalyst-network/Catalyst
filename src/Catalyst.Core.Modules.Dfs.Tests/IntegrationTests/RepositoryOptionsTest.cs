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
using System.IO;
using System.Linq;
using Catalyst.Abstractions.Options;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
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
                Assert.Equal($"{sep}home1{sep}.catalyst", options.Folder);

                Environment.SetEnvironmentVariable("HOME", $"{sep}home2{sep}");
                options = new RepositoryOptions();
                Assert.Equal($"{sep}home2{sep}.catalyst", options.Folder);
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
                Assert.Equal($"{sep}home1{sep}.catalyst", options.Folder);

                Environment.SetEnvironmentVariable("HOMEPATH", $"{sep}home2{sep}");
                options = new RepositoryOptions();
                Assert.Equal($"{sep}home2{sep}.catalyst", options.Folder);
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
