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
using System.Security;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Cryptography;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Registry
{
    public sealed class PasswordRegistryTests : IDisposable
    {
        private readonly SecureString _secureString = new SecureString();
        private readonly PasswordRegistry _passwordRegistry;

        public PasswordRegistryTests()
        {
            _passwordRegistry = new PasswordRegistry();
        }

        [Fact]
        public void Can_Add_Item_To_Registry()
        {
            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            _passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();
        }

        [Fact]
        public void Can_Add_Multiple_Items_To_Registry()
        {
            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.CertificatePassword, _secureString).Should().BeTrue();    
            
            _passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();
            _passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.CertificatePassword).Should().BeTrue();
        }

        [Fact]
        public void Cant_Add_Items_To_Registry_Twice_With_Same_Key()
        {
            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeFalse();    
            
            _passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();
        }

        [Fact]
        public void Can_Remove_Item_From_Registry()
        {
            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            _passwordRegistry.RemoveItemFromRegistry(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();    
            
            _passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeFalse();
        }

        [Fact]
        public void Can_Retrieve_Item_From_Registry()
        {
            _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            _passwordRegistry.GetItemFromRegistry(PasswordRegistryTypes.IpfsPassword).Should().BeEquivalentTo(_secureString);
        }

        [Fact]
        public void Retrieving_Item_Not_Contained_In_Registry_Returns_Null()
        {
            _passwordRegistry.GetItemFromRegistry(PasswordRegistryTypes.IpfsPassword).Should().BeEquivalentTo((SecureString) null);
        }

        [Fact]
        public void Cant_Add_Null_Item_To_Registry()
        {
            Action action = () =>
            {
                _passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.DefaultNodePassword, null);
            };
            action.Should().Throw<ArgumentNullException>();
        }

        public void Dispose()
        {
            _secureString.Dispose();
        }
    }
}
