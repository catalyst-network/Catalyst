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

﻿using System;
using System.IO;
using System.Runtime.Loader;
using Autofac;
using Autofac.Configuration;
using Catalyst.Node.Common.Interfaces;
using Microsoft.Extensions.Configuration;

using System.Diagnostics;

namespace Catalyst.Cli
{
    public static class Program
    {
        private static string CatalystSubfolder => ".Catalyst";
        private static string ShellFileName => "shell.json";

        /// <summary>
        ///     Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main()
        {
            const int bufferSize = 1024 * 67 + 128;

            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var catalystHomeDirectory = Path.Combine(homeDirectory, CatalystSubfolder);

            if (!Directory.Exists(catalystHomeDirectory))
            {
                Directory.CreateDirectory(catalystHomeDirectory);
            }

            // check if user home data dir has a shell config
            var shellFilePath = Path.Combine(catalystHomeDirectory, ShellFileName);
            if (!File.Exists(shellFilePath))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.shell.json"),
                    shellFilePath);
            }

            // resolve config from autofac
            var builder = new ContainerBuilder();

            AssemblyLoadContext.Default.Resolving += (context, assembly) =>
                context.LoadFromAssemblyPath(
                    Path.Combine(Directory.GetCurrentDirectory(),
                        $"{assembly.Name}.dll"));

            var shellConfig = new ConfigurationBuilder().AddJsonFile(shellFilePath)
               .Build();

            var shellModule = new ConfigurationModule(shellConfig);

            builder.RegisterModule(shellModule);

            var container = builder.Build();

            Console.SetIn(
                new StreamReader(
                    Console.OpenStandardInput(bufferSize),
                    Console.InputEncoding, false, bufferSize
                )
            );

            var shell = container.Resolve<IAds>();
            shell.RunConsole();
            return 0;
        }
    }
}