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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;

namespace Catalyst.Core.Cryptography
{
    public class ConsolePasswordReader : IPasswordReader
    {
        public static readonly int MaxLength = 255;

        private readonly IUserOutput _userOutput;
        private readonly IUserInput _userInput;
        
        public ConsolePasswordReader(IUserOutput userOutput, IUserInput userInput)
        {
            _userOutput = userOutput;
            _userInput = userInput;
        }

        public SecureString ReadSecurePassword(string prompt)
        {
            var pwd = new SecureString();
            ReadCharsFromConsole(prompt, (c, i) => pwd.AppendChar(c), i => pwd.RemoveAt(i));
            pwd.MakeReadOnly();
            return pwd;
        }

        private void ReadCharsFromConsole(string passwordContext,
            Action<char, int> appendChar,
            Action<int> removeChar)
        {
            _userOutput.WriteLine(passwordContext);

            var inputLength = 0;
            while (true)
            {
                var keyInfo = _userInput.ReadKey();
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    _userOutput.WriteLine(string.Empty);
                    break;
                }

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (inputLength == 0)
                    {
                        continue;
                    }

                    removeChar(inputLength - 1);
                    inputLength--;
                    _userOutput.Write(@" ");
                    continue;
                }

                appendChar(keyInfo.KeyChar, inputLength);
                inputLength++;
                _userOutput.Write(@"*");

                if (inputLength != MaxLength)
                {
                    continue;
                }

                _userOutput.WriteLine($"Max password length reached ({MaxLength})");
                break;
            }
        }
    }
}
