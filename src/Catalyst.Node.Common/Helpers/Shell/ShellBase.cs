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
using Catalyst.Node.Common.Interfaces;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Catalyst.Node.Common.Helpers.Shell
{
    public abstract class ShellBase : IShell
    {
        protected ShellBase()
        {
            AppCulture = new CultureInfo("en-GB", false);
        }

        private string Prompt => "Koopa";
        private string ServiceName => "Catalyst Distributed Shell";
        public static CultureInfo AppCulture { get; set; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStart(string[] args);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStartWork(string[] args);

        /// <summary>
        /// </summary>
        public abstract bool OnStop(string[] args);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStopNode(string[] args);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract bool OnStopWork(string[] args);

        /// <summary>
        ///     Prints a list of available cli commands.
        /// </summary>
        /// <returns></returns>
        protected bool OnHelpCommand(string advancedCmds = "")
        {
            var normalCmds =
                "Normal Commands:\n" +
                "\tstart work\n" +
                "\tstop node\n" +
                "\tstop work\n" +
                "\tget info\n" +
                "\tget config\n" +
                "\tget version\n" +
                "\thelp\n" +
                "\tclear\n" +
                "\texit\n";

            Console.WriteLine(normalCmds);

            if (advancedCmds != "")
            {
                Console.WriteLine(advancedCmds);
            }

            return true;
        }

        /// <summary>
        ///     Catalyst.Cli command service router.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual bool OnCommand(params string[] args)
        {
            switch (args[0].ToLower(AppCulture))
            {
                case "get":
                    return OnGetCommand(args);
                case "help":
                    return OnHelpCommand();
                case "clear":
                    Console.Clear();
                    return true;
                case "exit":
                    return false;
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool OnGetCommand(string[] args)
        {
            switch (args[1].ToLower(AppCulture))
            {
                case "config":
                    return OnGetConfig(args[2]);
                case "version":
                    return OnGetVersion(args.Skip(2).ToList());
                case "mempool":
                    return OnGetMempool(args.Skip(2).ToList());
                default:
                    return CommandNotFound(args);
            }
        }

        /// <summary>
        ///     Prints the current loaded settings.
        /// </summary>
        /// <returns></returns>
        protected abstract bool OnGetConfig(Object args);

        /// <summary>
        ///     Prints the current node version.
        /// </summary>
        /// <returns></returns>
        protected abstract bool OnGetVersion(Object args);

        /// <summary>
        ///     Prints stats about the mempool implementation.
        /// </summary>
        /// <returns></returns>
        protected abstract bool OnGetMempool(Object args);

        private static void ShellLogKey(ConsoleKeyInfo key)
        {
            Console.WriteLine(key.KeyChar.ToString());
            Console.WriteLine(' '.ToString());
            Console.WriteLine(key.KeyChar.ToString());
        }

        /// <summary>
        ///     Runs the main cli ui.
        /// </summary>
        /// <returns></returns>
        public bool RunConsole()
        {
            var running = true;

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
                string[] args = Regex.Split(line, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

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

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected static bool CommandNotFound(string[] args)
        {
            Console.WriteLine($@"error: command not found ${args}");
            return true;
        }
    }
}
