using System;
using ADL.Cli.Interfaces;

namespace ADL.Cli.Shell.Commands
{
    internal class PrintConfig
    {
        public static PrintConfig Default { get; }

        public void Print(INodeConfiguration config)
        {
            Console.WriteLine("print config");
//            Console.WriteLine(Environment.NewLine + "Application configuration: " + config.ApplicationValue);
//            Console.WriteLine("Component configuration: " + config.ComponentValue);
//            Console.WriteLine("Component configuration -> other configuration: " + config.OtherConfiguration?.OtherComponentValue);
        }

    }
}