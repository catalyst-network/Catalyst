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
using Catalyst.Abstractions.Types;
using Catalyst.Core.Keystore;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.Registry
{
    public class KeyRegistryTests
    {
        private readonly IPrivateKey _privateKey = Substitute.For<IPrivateKey>();
        private readonly KeyRegistry _keyRegistry;

        public KeyRegistryTests()
        {
            _keyRegistry = new KeyRegistry();
        }

        [Fact]
        public void Can_Add_Item_To_Registry()
        {
            _keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, _privateKey).Should().BeTrue();
            _keyRegistry.RegistryContainsKey(KeyRegistryTypes.DefaultKey).Should().BeTrue();
        }

        [Fact]
        public void Cant_Add_Items_To_Registry_Twice_With_Same_Key()
        {
            _keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, _privateKey).Should().BeTrue();
            _keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, _privateKey).Should().BeFalse();    
            
            _keyRegistry.RegistryContainsKey(KeyRegistryTypes.DefaultKey).Should().BeTrue();
        }

        [Fact]
        public void Can_Remove_Item_From_Registry()
        {
            _keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, _privateKey).Should().BeTrue();
            _keyRegistry.RemoveItemFromRegistry(KeyRegistryTypes.DefaultKey).Should().BeTrue();    
            
            _keyRegistry.RegistryContainsKey(KeyRegistryTypes.DefaultKey).Should().BeFalse();
        }

        [Fact]
        public void Can_Retrieve_Item_From_Registry()
        {
            _keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, _privateKey).Should().BeTrue();
            _keyRegistry.GetItemFromRegistry(KeyRegistryTypes.DefaultKey).Should().BeEquivalentTo(_privateKey);
        }

        [Fact]
        public void Retrieving_Item_Not_Contained_In_Registry_Returns_Null()
        {
            _keyRegistry.GetItemFromRegistry(KeyRegistryTypes.DefaultKey).Should().BeEquivalentTo((IPrivateKey) null);
        }

        [Fact]
        public void Cant_Add_Null_Item_To_Registry()
        {
            Action action = () =>
            {
                _keyRegistry.AddItemToRegistry(KeyRegistryTypes.DefaultKey, null);
            };
            action.Should().Throw<ArgumentNullException>();
        }
    }
}
