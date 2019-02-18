using System;
using System.IO;
using System.Runtime.Loader;
using Autofac;
using Autofac.Configuration;
using Catalyst.Node.Common.Shell;
using Microsoft.Extensions.Configuration;

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
        public static int Main(string[] args)
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
            if (!File.Exists(shellFilePath)) {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.shell.json"),
                    shellFilePath);}

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

            container.Resolve<IAds>();

            return 0;
        }
    }
}