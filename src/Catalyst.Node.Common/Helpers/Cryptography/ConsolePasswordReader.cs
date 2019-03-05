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
                        if (pwd.Length == 0) continue;
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