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
using System.Security;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Cryptography
{
    public class ConsolePasswordReader : IPasswordReader
    {
        //@TODO we have some duplication here between shell base
        public SecureString ReadSecurePassword(string passwordContext = "Please enter your password")
        {
            var pwd = new SecureString();
            ReadCharsFromConsole(passwordContext, (c, i) => pwd.AppendChar(c), i => pwd.RemoveAt(i));

            pwd.MakeReadOnly();
            return pwd;
        }

        public char[] ReadSecurePasswordAsChars(string passwordContext = "Please enter your password")
        {
            var maxLength = 255;
            var buffer = new char[maxLength];
            var length = ReadCharsFromConsole(passwordContext,
                (c, i) => buffer[i] = c,
                i => { buffer[i] = default; },
                maxLength);
            var password = buffer.Take(length).ToArray();

            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = default;
            }

            return password;
        }

        private static int ReadCharsFromConsole(string passwordContext,
            Action<char, int> appendChar,
            Action<int> removeChar,
            int maxLength = int.MaxValue)
        {
            Console.WriteLine(passwordContext);
            var waitForInput = true;
            var inputLength = 0;
            while (waitForInput)
            {
                var keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine(string.Empty);
                        waitForInput = false;
                        break;

                    case ConsoleKey.Backspace:
                        if (inputLength == 0)
                        {
                            continue;
                        }

                        removeChar(inputLength - 1);
                        inputLength--;
                        Console.Write("\b \b");
                        break;

                    default:
                        appendChar(keyInfo.KeyChar, inputLength);
                        inputLength++;
                        Console.Write(@"*");
                        if (inputLength == maxLength)
                        {
                            Console.WriteLine(@"Max password length reached ({0})", maxLength);
                            waitForInput = false;
                        }

                        break;
                }
            }

            return inputLength;
        }
    }
}
