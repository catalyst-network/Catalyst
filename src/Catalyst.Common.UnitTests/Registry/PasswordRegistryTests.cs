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
using Catalyst.Common.Registry;
using Catalyst.Common.Types;
using FluentAssertions;
using Xunit;

namespace Catalyst.Common.UnitTests.Registry
{
    public sealed class PasswordRegistryTests : IDisposable
    {
        private readonly SecureString _secureString = new SecureString();
        private readonly PasswordRegistry passwordRegistry;

        public PasswordRegistryTests()
        {
            passwordRegistry = new PasswordRegistry();
        }

        [Fact]
        public void Can_Add_Item_To_Registry()
        {
            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();
        }

        [Fact]
        public void Can_Add_Multiple_Items_To_Registry()
        {
            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.CertificatePassword, _secureString).Should().BeTrue();    
            
            passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();
            passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.CertificatePassword).Should().BeTrue();
        }

        [Fact]
        public void Cant_Add_Items_To_Registry_Twice_With_Same_Key()
        {
            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeFalse();    
            
            passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();
        }

        [Fact]
        public void Can_Remove_Item_From_Registry()
        {
            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            passwordRegistry.RemoveItemFromRegistry(PasswordRegistryTypes.IpfsPassword).Should().BeTrue();    
            
            passwordRegistry.RegistryContainsKey(PasswordRegistryTypes.IpfsPassword).Should().BeFalse();
        }

        [Fact]
        public void Can_Retrieve_Item_From_Registry()
        {
            passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.IpfsPassword, _secureString).Should().BeTrue();
            passwordRegistry.GetItemFromRegistry(PasswordRegistryTypes.IpfsPassword).Should().BeEquivalentTo(_secureString);
        }

        [Fact]
        public void Retrieving_Item_Not_Contained_In_Registry_Returns_Null()
        {
            passwordRegistry.GetItemFromRegistry(PasswordRegistryTypes.IpfsPassword).Should().BeEquivalentTo((SecureString) null);
        }

        [Fact]
        public void Cant_Add_Null_Item_To_Registry()
        {
            Action action = () =>
            {
                passwordRegistry.AddItemToRegistry(PasswordRegistryTypes.DefaultNodePassword, null);
            };
            action.Should().Throw<ArgumentNullException>();
        }

        public void Dispose()
        {
            _secureString.Dispose();
        }
    }
}
