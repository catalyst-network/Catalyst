using System;
using CommandLine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Modules.UPnP
{
    static class Program
    {

        static async Task Main(string[] args)
        {
            const int timeoutInSeconds = 10;
            var provider = new NatUtilityProvider();
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            var portMapper = new PortMapper(provider, logger);
            var portMappings = new []{new Mapping(Protocol.Tcp, 6025, 6025)};
            await portMapper.AddPortMappings(portMappings, timeoutInSeconds);
        }
        
        public static List<Mapping> LoadPortMappings(string path)
        {
            using var r = new StreamReader(path);
            var json = r.ReadToEnd();
            return JsonConvert.DeserializeObject<List<Mapping>>(json);
        }



    }
}
