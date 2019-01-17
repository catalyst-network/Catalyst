using System;
using Autofac;
using Catalyst.Helpers.Shell;
using System.IO;
using System.Net;
using Catalyst.Helpers.Exceptions;
using Catalyst.Helpers.FileSystem;
using System.Reflection;
using System.Runtime.Loader;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Cli
{
    public class Program
    {
        private static uint Env { get; set; }
        private static uint Port { get; set; }
        private static string Network { get; set; }
        private static IPAddress Host { get; set; }
        private static string DataDir { get; set; }
        private static uint MaxOutConnections { get; set; }       

        /// <summary>
        /// Main cli loop
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            const int bufferSize = 1024 * 67 + 128;

            AppDomain.CurrentDomain.UnhandledException += Unhandled.UnhandledException;

            if (!Directory.Exists(Fs.GetUserHomeDir() + "/.Catalyst"))
            {
                Directory.CreateDirectory(Fs.GetUserHomeDir() + "/.Catalyst");
            }

            // check if user home data dir has a shell config
            if (!File.Exists(Fs.GetUserHomeDir() + "/.Catalyst/shell.json"))
            {
                // copy skeleton configs to default data dir
                File.Copy(AppDomain.CurrentDomain.BaseDirectory +"/config.shell.json", Fs.GetUserHomeDir()+"/.Catalyst/shell.json");
            }

            // resolve config from autofac
            var builder = new ContainerBuilder();
            
            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assembly) =>
                context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.dll"));
            
            var shellConfig = new ConfigurationBuilder().AddJsonFile(Fs.GetUserHomeDir() + "/.Catalyst/shell.json").Build();
            
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
