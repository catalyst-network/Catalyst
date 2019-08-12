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

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Catalyst.TestUtils
{
    public abstract class ConfigFileBasedTest : FileSystemBasedTest
    {
        protected List<string> ConfigFilesUsed { get; }

        protected ContainerProvider ContainerProvider;

        protected ConfigFileBasedTest(IEnumerable<string> configFilesUsed, ITestOutputHelper output) : base(output)
        {
            ConfigFilesUsed = configFilesUsed as List<string> ?? configFilesUsed?.ToList() ?? new List<string>();
            ContainerProvider = new ContainerProvider(ConfigFilesUsed, FileSystem, Output);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            ContainerProvider?.Dispose();
        }
    }
}
