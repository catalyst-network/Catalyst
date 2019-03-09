/*
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

using System;
using System.Security;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Cryptography
{
    public class ConsolePasswordReader : IPasswordReader
    {
        //@TODO we have some duplication here between shell base
        public SecureString ReadSecurePassword(string passwordContext = "Please enter your password")
        {
            Console.WriteLine(passwordContext);
            var pwd = new SecureString();
            var waitForInput = true;
            while (waitForInput)
            {
                var keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine("");
                        waitForInput = false;
                        break;

                    case ConsoleKey.Backspace:
                        if (pwd.Length == 0)
                        {
                            continue;
                        }
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                        break;

                    default:
                        pwd.AppendChar(keyInfo.KeyChar);
                        Console.Write(@"*");
                        break;
                }
            }

            pwd.MakeReadOnly();
            return pwd;
        }
    }
}