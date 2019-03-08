/*
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

﻿using System.Collections.Generic;
using System.Linq;
using Catalyst.Node.Common.Helpers.Enumerator;
 using Catalyst.Node.Common.Modules;
 using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Modules
{
    public static class ModuleNamesTests
    {
        [Fact]
        public static void All_should_return_all_declared_names()
        {
            var allModuleNames = Enumeration.GetAll<ModuleName>().Select(m => m.Name);

            var expectedList = new List<string>
                {"Consensus", "Contract", "Dfs", "Gossip", "Ledger", "Mempool", "KeySigner"};

            allModuleNames.Should().BeEquivalentTo(expectedList);
        }
    }
}