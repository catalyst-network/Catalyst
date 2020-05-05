using System;
using CommandLine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Modules.UPnP
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var timeoutInSeconds = 10;
            INatUtilityProvider provider = new NatUtilityProvider();
            var portMapper = new PortMapper(provider);
            portMapper.TryGetDevice(5, timeoutInSeconds);
            portMapper.TimeoutReached += Exit;
        }

        private static void Exit(object sender, EventArgs e)
            {
                Console.WriteLine("exiting");
            }
        }
}
