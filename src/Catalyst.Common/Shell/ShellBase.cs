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
using System.Reflection;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using Catalyst.Common.Interfaces.Cli;

namespace Catalyst.Common.Shell
{
    public abstract class ShellBase : IShell
    {
        protected ShellBase() { }

        private static string Prompt => "Koopa";
        private static string ServiceName => "Catalyst Distributed Shell";
        private static CultureInfo AppCulture => new CultureInfo("en-GB", false);
        
        /// <inheritdoc />
        /// <summary>
        ///     Runs the main cli ui.
        /// </summary>
        /// <returns></returns>
        public bool RunConsole()
        {
            const bool running = true;

            Console.OutputEncoding = Encoding.Unicode;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            var ver = Assembly.GetEntryAssembly().GetName().Version;
            Console.WriteLine($@"{ServiceName} Version: {ver}");

            while (running)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine($@"{Prompt}> ");

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                var line = Console.ReadLine()?.Trim();
                if (line == null)
                {
                    break;
                }

                Console.ForegroundColor = ConsoleColor.White;

                //split the command line input by spaces and keeping hyphens and preserve any spaces between quotes
                var args = Regex.Split(line, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                if (args.Length == 0)
                {
                    continue;
                }

                try
                {
                    ParseCommand(args);
                }
                catch (SystemException ex)
                {
                    Console.WriteLine($@"Exception raised in Shell ${ex.Message}");
                }
            }

            Console.ResetColor();
            return running;
        }

        public abstract bool ParseCommand(params string[] args);
    }
}
