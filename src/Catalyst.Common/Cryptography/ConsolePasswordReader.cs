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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Common.Types;

namespace Catalyst.Common.Cryptography
{
    public class ConsolePasswordReader : IPasswordReader
    {
        private const int MaxLength = 255;

        private readonly IUserOutput _userOutput;
        private readonly IPasswordRegistry _passwordRegistry;
        
        public ConsolePasswordReader(IUserOutput userOutput, IPasswordRegistry passwordRegistry) 
        { 
            _userOutput = userOutput;
            _passwordRegistry = passwordRegistry;
        }
        
        public SecureString ReadSecurePasswordAndAddToRegistry(PasswordRegistryTypes passwordIdentifier, string prompt = "Please enter your password")
        {
            var password = ReadSecurePassword(passwordIdentifier, prompt);
            if (password != null)
            {
                AddPasswordToRegistry(passwordIdentifier, password);
            }

            return password;
        }

        public SecureString ReadSecurePassword(PasswordRegistryTypes passwordIdentifier, string prompt = "Please enter your password")
        {
            var password = _passwordRegistry.GetItemFromRegistry(passwordIdentifier) ??
                ReadSecurePasswordFromConsole(prompt);

            return password;
        }
                   
        private SecureString ReadSecurePasswordFromConsole(string prompt)
        {
            var pwd = new SecureString();
            ReadCharsFromConsole(_userOutput, prompt, (c, i) => pwd.AppendChar(c), i => pwd.RemoveAt(i));
            pwd.MakeReadOnly();
            return pwd;
        }

        public bool AddPasswordToRegistry(PasswordRegistryTypes passwordIdentifier, SecureString password)
        {
            return _passwordRegistry.AddItemToRegistry(passwordIdentifier, password);
        }

        private static void ReadCharsFromConsole(IUserOutput userOutput,
            string passwordContext,
            Action<char, int> appendChar,
            Action<int> removeChar,
            int maxLength = MaxLength)
        {
            Console.WriteLine(passwordContext);
            var waitForInput = true;
            var inputLength = 0;
            while (waitForInput)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key != ConsoleKey.Enter)
                {
                    if (keyInfo.Key != ConsoleKey.Backspace)
                    {
                        appendChar(keyInfo.KeyChar, inputLength);
                        inputLength++;
                        userOutput.Write(@"*");
                        if (inputLength != maxLength)
                        {
                            continue;
                        }

                        userOutput.WriteLine($"Max password length reached ({maxLength})");
                        waitForInput = false;
                    }
                    else
                    {
                        if (inputLength == 0)
                        {
                            continue;
                        }

                        removeChar(inputLength - 1);
                        inputLength--;
                        userOutput.Write(@" ");
                    }
                }
                else
                {
                    userOutput.WriteLine(string.Empty);
                    waitForInput = false;
                }
            }
        }
    }
}
