using System;
using CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mono.Nat;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Modules.UPnP
{
    internal class Options
    {
        [Option('t', "timeout", HelpText = "The delay in seconds after which this application will stop.")]
        public int Timeout { get; set; }
            
        [Option("filepath", Required = false, HelpText = "The path of the mapping file")]
        public string FilePath { get; set; }

        [Option("tcp", Default = PortMapperConstants.DefaultTcpProperty,
            HelpText = "The identifier of the tcp ports to be mapped; multiple identifiers can be provided using comma separators")]
        public string TcpProperties { get; set; }
        
        [Option( "udp", Default = PortMapperConstants.DefaultUdpProperty,
            HelpText = "The identifier of the udp ports to be mapped; multiple identifiers can be provided using comma separators")]
        public string UdpProperties { get; set; }
        
        [Option( "delete", Default = false, HelpText = "To remove rather than add the specified port mappings")]
        public bool IsMappingDeletion { get; set; }
    }

    public static partial class Program
    {
        public static Task<int> Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            return Start(args, logger, new NatUtilityProvider());
        }

        public static async Task<int> Start(string[] args, ILogger logger, INatUtilityProvider natUtilityProvider)
        {
            var result = await Parser.Default
                .ParseArguments<Options>(args).MapResult(async options => await RunPortMapper(options, logger, natUtilityProvider),
                    response =>  Task.FromResult(1)).ConfigureAwait(false);

            return Environment.ExitCode = result;
        }

        private static async Task<int> RunPortMapper(Options options, ILogger logger, INatUtilityProvider provider)
        {
            var json = LoadFileWithLogging(options.FilePath, logger);
            if (json==null) return 0;

            var mappings = ParseJsonWithLogging(options.TcpProperties, options.UdpProperties, json, logger);
            if (!(mappings?.Count > 0)) return 0;
            
            var portMapper = new PortMapper(provider, logger);
            
            var timeout = options.Timeout > 0 ? options.Timeout : PortMapperConstants.DefaultTimeout;

            await portMapper.MapPorts(mappings.ToArray(), timeout, options.IsMappingDeletion);
            return 0;
        }

        private static string LoadFileWithLogging(string path, ILogger logger)
        {
            try
            {
                using var r = new StreamReader(path);
                return r.ReadToEnd();
            }
            catch(Exception)
            {
                logger.Error("Unable to find or read the file provided.");
            }
            return null;
        }
        
        private static List<Mapping> ParseJsonWithLogging(string tcp, string udp, string json, ILogger logger)
        {
            try
            {
                return PortMappingParser.ParseJson(tcp, udp, json);
            }
            catch(JsonReaderException)
            {
                logger.Error("The file did not contain valid json");
                return null;
            }
        }
    }
}
