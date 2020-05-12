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
using Newtonsoft.Json.Linq;
using Serilog;

namespace Catalyst.Modules.UPnP
{
    internal class Options
    {
        [Option('t', "timeout", HelpText = "The delay in seconds after which this application will stop.")]
        public int Timeout { get; set; }
            
        [Option('p', "filepath", HelpText = "The path of the mapping file")]
        public string FilePath { get; set; }
    }

    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var result = await Parser.Default
                .ParseArguments<Options>(args).MapResult(async options => await RunPortMapper(options),
                    response =>  Task.FromResult(1)).ConfigureAwait(false);

            return Environment.ExitCode = result;
        }

        private static async Task<int> RunPortMapper(Options options)
        {
            var provider = new NatUtilityProvider();
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            var portMapper = new PortMapper(provider, logger);
            var mappings = LoadPortMappingConfig(options.FilePath);
            return await portMapper.AddPortMappings(mappings.ToArray(), options.Timeout>0?options.Timeout:10);
        }
        
        private static List<Mapping> LoadPortMappingConfig(string path)
        {
            //using var r = new StreamReader(path);
            //var json = r.ReadToEnd();
            const string json = @"[{'Protocol':0,'PrivatePort':6024,'PublicPort':6024}]";
            return JsonConvert.DeserializeObject<List<Mapping>>(json, new MappingConverter()) ?? new List<Mapping>();
        }



    }
}
