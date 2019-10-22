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
using System.Reflection;
using Autofac;
using Catalyst.Abstractions.Cli;
using Catalyst.Core.Lib;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Protocol.Network;
using Catalyst.Tools.KeyGenerator.Commands;
using Catalyst.Tools.KeyGenerator.Core;
using Catalyst.Tools.KeyGenerator.Interfaces;
using CommandLine;
using Serilog;

namespace Catalyst.Tools.KeyGenerator
{
    internal static class Program
    {
        private static IKeyGeneratorCommand[] _commands;
        private static ILogger _logger;
        private static IUserOutput _userOutput;

        public static void Main(string[] args)
        {
            var containerBuilder = new ContainerBuilder();

            // Modules
            containerBuilder.RegisterModule(new CoreLibProvider());
            containerBuilder.RegisterModule(new KeystoreModule());
            containerBuilder.RegisterModule(new KeySignerModule());
            containerBuilder.RegisterModule(new BulletProofsModule());
            containerBuilder.RegisterModule(new HashingModule());

            // Commands
            containerBuilder.RegisterType<GenerateKeyStore>().As<IKeyGeneratorCommand>();
            containerBuilder.RegisterType<LoadKeyStore>().As<IKeyGeneratorCommand>();

            // Core
            containerBuilder.RegisterType<ConsoleUserOutput>().As<IUserOutput>();
            containerBuilder.RegisterType<ConsoleUserInput>().As<IUserInput>();
            containerBuilder.RegisterType<PasswordRegistryLoader>().As<IPasswordRegistryLoader>();

            _logger = new LoggerConfiguration()
               .WriteTo.Console()
               .CreateLogger()
               .ForContext(MethodBase.GetCurrentMethod().DeclaringType);
            
            containerBuilder.RegisterInstance(_logger).As<ILogger>();

            var container = containerBuilder.Build();

            _userOutput = container.Resolve<IUserOutput>();
            _commands = container.Resolve<IKeyGeneratorCommand[]>();

            while (true)
            {
                try
                {
                    ParseCommand(args);
                }
                catch (Exception e)
                {
                    _logger.Error(e, nameof(ParseCommand));
                }

                // Non-interactive command
                if (args.Length != 0)
                {
                    break;
                }
            }
        }

        private static void ParseCommand(string[] args)
        {
            var interactive = args.Length == 0;

            if (interactive)
            {
                _userOutput.WriteLine($"___________________________________________________________{'\n'}");
                _userOutput.Write("Enter Command: ");
            }

            var commandArgs = interactive ? Console.ReadLine()?.Split(" ") : args;

            if (commandArgs == null)
            {
                return;
            }

            if (commandArgs[0].Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            {
                Environment.Exit(0);
                return;
            }

            var parsedCommand = _commands.FirstOrDefault(command =>
                command.CommandName.Equals(commandArgs[0], StringComparison.InvariantCultureIgnoreCase));
            var commandExists = parsedCommand != null;

            if (commandExists)
            {
                var parserResult = Parser.Default.ParseArguments(commandArgs, parsedCommand.OptionType);
                if (parserResult.Tag == ParserResultType.NotParsed)
                {
                    return;
                }

                parserResult.WithParsed(options => parsedCommand.ParseOption(NetworkType.Devnet, options)); //@TODO can't hard code this
                return;
            }

            var types = _commands.Select(command => command.OptionType).ToArray();
            Parser.Default.ParseArguments(commandArgs, types);
        }
    }
}
