using System;
using System.ComponentModel;
using System.Text;
using ADL.Node.Interfaces;

namespace ADL.Cli.Shell.Commands
{
    internal class PrintConfig
    {
        /// <summary>
        /// Prints current contexts loaded configuration.
        /// </summary>
        /// <param name="config"></param>
        public void Print(INodeConfiguration config)
        {
            foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(config))
            {
                string name=descriptor.Name;
                object value=descriptor.GetValue(config);
                Console.WriteLine("{0}={1}",name,value);
            }
        }
    }
}