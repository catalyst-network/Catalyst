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
using Catalyst.Core.Cli;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Shell
{
    public sealed class ConsoleUserOutputTests
    {
        [Fact]
        public void WriteLine_Should_WriteLine_To_Console()
        {
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                const string message = "Hello world";
                var consoleUserOutput = new ConsoleUserOutput();

                consoleUserOutput.WriteLine(message);

                sw.ToString().Should().Be($"{message}{Environment.NewLine}");
            }
        }

        [Fact]
        public void Write_Should_Write_To_Console()
        {
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                const string message = "Hello world";
                var consoleUserOutput = new ConsoleUserOutput();

                consoleUserOutput.Write(message);

                sw.ToString().Should().Be(message);
            }
        }
    }
}
