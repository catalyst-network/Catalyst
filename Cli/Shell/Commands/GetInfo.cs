using System;
using ADL.Node;

namespace ADL.Cli.Shell.Commands
{
    internal static class GetInfo
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
