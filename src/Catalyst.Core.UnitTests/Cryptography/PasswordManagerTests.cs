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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Cryptography;
using FluentAssertions;
using Humanizer;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.Cryptography
{
    public sealed class PasswordManagerTests : IDisposable
    {
        private readonly IPasswordRegistry _passwordRegistry;
        private readonly IPasswordReader _passwordReader;
        private readonly PasswordManager _passwordManager;
        private readonly SecureString _secureString;

        public PasswordManagerTests()
        {
            _passwordRegistry = Substitute.For<IPasswordRegistry>();
            _passwordReader = Substitute.For<IPasswordReader>();

            _secureString = new SecureString();

            _passwordManager = new PasswordManager(_passwordReader, _passwordRegistry);
        }

        [Fact]
        public void ReadAndAddPasswordToRegistry_Should_Prompt_And_Add_Non_Null_Passwords()
        {
            _passwordReader.ReadSecurePassword().ReturnsForAnyArgs(_secureString);

            var registryType = PasswordRegistryTypes.CertificatePassword;
            _passwordReader.ReadSecurePassword(Arg.Any<string>()).Returns(_secureString);

            var retrievedPassword = _passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(registryType);

            _passwordReader.Received(1).ReadSecurePassword(Arg.Is<string>(s => s.Contains(registryType.Name.Humanize())));
            _passwordRegistry.Received(1).AddItemToRegistry(registryType, _secureString);

            retrievedPassword.Should().Be(_secureString);
        }

        [Fact]
        public void ReadAndAddPasswordToRegistry_Should_Prompt_And_Not_Add_Null_Passwords()
        {
            _passwordReader.ReadSecurePassword().ReturnsForAnyArgs(_secureString);

            var registryType = PasswordRegistryTypes.IpfsPassword;
            _passwordReader.ReadSecurePassword(Arg.Any<string>()).Returns((SecureString) null);

            var retrievedPassword = _passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(registryType);

            _passwordReader.Received(1).ReadSecurePassword(Arg.Is<string>(s => s.Contains(registryType.Name.Humanize())));
            _passwordRegistry.DidNotReceiveWithAnyArgs().AddItemToRegistry(default, default);

            retrievedPassword.Should().BeNull();
        }

        [Fact]
        public void AddPasswordToRegistry_Should_Add_Passwords_To_The_Correct_Registry()
        {
            _passwordReader.ReadSecurePassword().ReturnsForAnyArgs(_secureString);
            _passwordRegistry.AddItemToRegistry(default, default).ReturnsForAnyArgs(true);

            var registryType = PasswordRegistryTypes.DefaultNodePassword;
            
            var allGood = _passwordManager.AddPasswordToRegistry(registryType, _secureString);

            _passwordRegistry.Received(1).AddItemToRegistry(registryType, _secureString);
            allGood.Should().BeTrue();
        }

        [Fact]
        public void RetrieveOrPromptPassword_When_Pass_In_Registry_Should_Not_Prompt_Password_From_Console()
        {
            var registryType = PasswordRegistryTypes.DefaultNodePassword;
            _passwordRegistry.GetItemFromRegistry(
                Arg.Is(registryType)).Returns(_secureString);

            var retrievedPassword = _passwordManager.RetrieveOrPromptPassword(registryType);

            _passwordReader.DidNotReceiveWithAnyArgs().ReadSecurePassword(default);
            retrievedPassword.Should().Be(_secureString);
        }

        [Fact]
        public void RetrieveOrPromptPassword_When_Pass_Not_In_Registry_Should_Prompt_Password_From_Console()
        {
            var registryType = PasswordRegistryTypes.IpfsPassword;
            
            _passwordRegistry.GetItemFromRegistry(
                Arg.Is(registryType)).Returns((SecureString) null);
            _passwordReader.ReadSecurePassword().ReturnsForAnyArgs(_secureString);

            var retrievedPassword = _passwordManager.RetrieveOrPromptPassword(registryType);

            _passwordReader.Received(1).ReadSecurePassword(Arg.Is<string>(s => s.Contains(registryType.Name.Humanize())));
            retrievedPassword.Should().Be(_secureString);
        }

        public void Dispose() { _secureString?.Dispose(); }
    }
}

