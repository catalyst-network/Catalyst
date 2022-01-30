#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.Tests.UnitTests.Cryptography
{
    public class PasswordRepeaterTests
    {
        private IPasswordRepeater _passwordRepeater;
        private IKeyApi _keyApi;
        private IUserOutput _userOutput;
        private IPasswordManager _passwordManager;
        private SecureString _password;

        [SetUp]
        public void Init()
        {
            _keyApi = Substitute.For<IKeyApi>();
            _userOutput = Substitute.For<IUserOutput>();

            _password = TestPasswordReader.BuildSecureStringPassword("test");

            _passwordManager = Substitute.For<IPasswordManager>();
            _passwordRepeater = new NodePasswordRepeater(_keyApi, _passwordManager, _userOutput);
        }

        [Test]
        public async Task PasswordRepeater_Should_Return_Password()
        {
            _passwordManager.PromptPassword(Arg.Is(PasswordRegistryTypes.DefaultNodePassword), Arg.Any<string>()).Returns(_password);

            var password = await _passwordRepeater.PromptAndReceiveAsync();

            password.Should().Be(_password);
        }

        [Test]
        public async Task PasswordRepeater_Should_Add_Password_To_Password_Registry()
        {
            _passwordManager.PromptPassword(Arg.Is(PasswordRegistryTypes.DefaultNodePassword), Arg.Any<string>()).Returns(_password);

            await _passwordRepeater.PromptAndAddPasswordToRegistryAsync();

            _passwordManager.Received(1).AddPasswordToRegistry(Arg.Is(PasswordRegistryTypes.DefaultNodePassword), Arg.Is(_password));
        }

        [Test]
        public async Task PasswordRepeater_Should_Ask_For_Password_When_Password_Is_Invalid()
        {
            var attempts = 0;
            var maxAttempts = 3;
            _keyApi.SetPassphraseAsync(Arg.Is(_password)).Throws<UnauthorizedAccessException>();
            _passwordManager.PromptPassword(Arg.Is(PasswordRegistryTypes.DefaultNodePassword), Arg.Any<string>()).Returns(_password).AndDoes(x =>
            {
                //Attempt 3 wrong password, and then the correct password.
                if (attempts >= maxAttempts)
                {
                    _keyApi.SetPassphraseAsync(Arg.Is(_password)).Returns(Task.CompletedTask);
                }
                attempts++;
            });

            var password = await _passwordRepeater.PromptAndReceiveAsync();

            _userOutput.Received(maxAttempts).WriteLine($"Invalid node password, please try again.");

            password.Should().Be(_password);
        }
    }
}
