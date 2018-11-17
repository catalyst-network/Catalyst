using System;
using ADL.Node;

namespace ADL.Cli.Shell.Commands
{
    internal static class GetConfig
    {
        /// <summary>
        /// Prints current contexts loaded configuration.
        /// </summary>
        public static void Print()
        {
            var settings = Settings.Default;
            Console.WriteLine(settings.SerializeSettings());
        }
    }
}
