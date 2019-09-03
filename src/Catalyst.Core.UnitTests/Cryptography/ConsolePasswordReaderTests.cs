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
using System.Linq;
using Catalyst.Abstractions.Cli;
using Catalyst.Core.Cryptography;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.Cryptography
{
    public class ConsolePasswordReaderTests
    {
        private readonly IUserInput _userInput;
        private readonly IUserOutput _userOutput;
        private readonly ConsolePasswordReader _consolePasswordReader;

        public ConsolePasswordReaderTests()
        {
            _userInput = Substitute.For<IUserInput>();
            _userOutput = Substitute.For<IUserOutput>();

            _consolePasswordReader = new ConsolePasswordReader(_userOutput, _userInput);
        }

        [Fact]
        public void ReadSecurePassword_Should_Prompt_Context_And_Get_Password_From_Console()
        {
            var prompt = "hello give me a password";
            var keysPressed = new[]
            {
                new ConsoleKeyInfo('?', ConsoleKey.Backspace, false, false, false),
                new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false),
                new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false),
                new ConsoleKeyInfo('?', ConsoleKey.Backspace, false, false, false),
                new ConsoleKeyInfo('@', ConsoleKey.A, false, false, false),
                new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false),
                new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false),
                new ConsoleKeyInfo('?', ConsoleKey.Enter, false, false, false)
            };
            _userInput.ReadKey().Returns(keysPressed.First(), keysPressed.Skip(1).ToArray());

            using (var pass = _consolePasswordReader.ReadSecurePassword(prompt))
            {
                _userOutput.Received(1).WriteLine(prompt);
                pass.Length.Should().Be(4);
                pass.IsReadOnly().Should().BeTrue();
            }
        }

        [Fact]
        public void ReadSecurePassword_Should_Not_Accept_Password_Above_MaxLength_Chars()
        {
            var prompt = "hello give me a password";
            var keysPressed = Enumerable.Repeat(0, ConsolePasswordReader.MaxLength + 1)
               .Select(_ => new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false))
               .ToArray();

            _userInput.ReadKey().Returns(keysPressed.First(), keysPressed.Skip(1).ToArray());

            using (var pass = _consolePasswordReader.ReadSecurePassword(prompt))
            {
                _userOutput.Received(1).WriteLine(
                    Arg.Is<string>(s => s.Contains(ConsolePasswordReader.MaxLength.ToString())));
                pass.Length.Should().Be(255);
                pass.IsReadOnly().Should().BeTrue();
            }
        }
    }
}

