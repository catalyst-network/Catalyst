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
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Catalyst.Abstractions.Cli;

namespace Catalyst.Cli
{
    public abstract class CatalystCliBase
        : ICatalystCli
    {
        protected readonly IUserOutput UserOutput;

        protected CatalystCliBase(IUserOutput userOutput)
        {
            UserOutput = userOutput;
        }

        private static string Prompt => "Koopa";
        private static string ServiceName => "Catalyst Distributed Shell";
        private static CultureInfo AppCulture => new CultureInfo("en-GB", false);

        /// <inheritdoc />
        public bool RunConsole(CancellationToken ct)
        {
            const bool running = true;

            Console.OutputEncoding = Encoding.Unicode;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            var ver = Assembly.GetEntryAssembly()?.GetName().Version;
            UserOutput.WriteLine($@"{ServiceName} Version: {ver}");

            while (!ct.IsCancellationRequested)
            {
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                UserOutput.WriteLine($@"{Prompt}> ");

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
                    UserOutput.WriteLine($@"Exception raised in Shell ${ex.Message}");
                }
            }

            Console.ResetColor();
            return running;
        }

        /// <inheritdoc />
        public abstract bool ParseCommand(params string[] args);
    }
}
