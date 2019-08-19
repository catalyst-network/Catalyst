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
using System.Linq;
using System.Security;
using System.Transactions;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Common.Types;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.Cryptography
{
    public class ConsolePasswordReaderTests : IDisposable
    {
        private Stream _inputStream;
        private readonly IPasswordRegistry _passwordRegistry;
        private readonly IUserOutput _userOutput;
        private TextReader _consoleInput;
        private readonly ConsolePasswordReader _consolePasswordReader;

        public ConsolePasswordReaderTests()
        {
            _passwordRegistry = Substitute.For<IPasswordRegistry>();
            _userOutput = Substitute.For<IUserOutput>();
            _consoleInput = Console.In;

            _consolePasswordReader = new ConsolePasswordReader(_userOutput, _passwordRegistry);
        }

        [Fact]
        public void ReadSecurePasswordAndAddToRegistry_When_Pass_In_Registry_Should_Not_Prompt_Password_From_Console()
        {
            var registryType = PasswordRegistryTypes.DefaultNodePassword;
            _passwordRegistry.GetItemFromRegistry(Arg.Is(registryType)).Returns(new SecureString());

            _consolePasswordReader.ReadSecurePasswordAndAddToRegistry(registryType);

            _userOutput.DidNotReceiveWithAnyArgs().WriteLine(default);
        }

        [Fact]
        public void ReadSecurePasswordAndAddToRegistry_When_Pass_Not_In_Registry_Should_Prompt_Password_From_Console()
        {
            //var registryType = PasswordRegistryTypes.DefaultNodePassword;
            //_passwordRegistry.GetItemFromRegistry(Arg.Is(registryType)).Returns(new SecureString());

            //using (var inputMemStream = new MemoryStream())
            //using (var inputWriter = new StreamWriter(inputMemStream))
            //using (var inputReader = new StreamReader(inputMemStream))
            //{
            //    Console.SetIn(inputReader);
            //    Interop.
            //    "passwe\bord\r\n".ToList().ForEach(c => inputWriter.Write(c));

            //    _consolePasswordReader.ReadSecurePasswordAndAddToRegistry(registryType);
            //    _userOutput.ReceivedWithAnyArgs(1).WriteLine(default);
            //}
        }

        public void Dispose()
        {
        }
    }
}

