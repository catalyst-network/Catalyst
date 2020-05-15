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

using Catalyst.TestUtils;
using FluentAssertions;
using NUnit.Framework;

/// <summary>
///     This class is only here to fix this issue
///     https://github.com/xunit/xunit/issues/1314
///     Basically all projects need to have at least one test of each trait
///     if a negative filter against this trait is needed. We can't really
///     expect people to cater for that bug, hopefully it will get fixed and
///     in the meantime this should automate the fix on all test projects
///     (cf. Common.TestProjects.props)
/// </summary>
public class XunitTraitsRegister
{
    [Test]
   
    public void IntegrationTest() { true.Should().BeTrue(); }

    [Test]
    [Category(Traits.EmbeddedChannelTest)]
    public void EmbeddedChannelTest() { true.Should().BeTrue(); }

    [Test]
    [Category(Traits.E2ECosmosDb)]
    public void E2E_CosmosDB() { true.Should().BeTrue(); }

    [Test]
    [Category(Traits.E2EMongoDb)]
    public void E2E_MongoDB() { true.Should().BeTrue(); }

    [Test]
    [Category(Traits.E2EMssql)]
    public void E2E_MSSQL() { true.Should().BeTrue(); }
}
