#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/
#endregion

using System;
using CommandLine;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;
using Serilog;
using Newtonsoft.Json;
using Catalyst.Modules.UPnP;

namespace Catalyst.Modules.AutoPortMapper
{
    internal class Options
    {
        [Option('t', "timeout", HelpText = "The delay in seconds after which this application will stop.")]
        public int Timeout { get; set; }
            
        [Option("filepath", Required = false, HelpText = "The path of the mapping file")]
        public string FilePath { get; set; }

        [Option("tcp", Default = PortMappingConstants.DefaultTcpProperty,
            HelpText = "The identifier of the tcp ports to be mapped; multiple identifiers can be provided using comma separators")]
        public string TcpProperties { get; set; }
        
        [Option( "udp", Default = PortMappingConstants.DefaultUdpProperty,
            HelpText = "The identifier of the udp ports to be mapped; multiple identifiers can be provided using comma separators")]
        public string UdpProperties { get; set; }
        
        [Option( "delete", Default = false, HelpText = "To remove rather than add the specified port mappings")]
        public bool IsMappingDeletion { get; set; }
    }

    public static class Program
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
            
            var portMapper = new UPnPUtility(provider, logger);

            var cts = new CancellationTokenSource();
            
            var timeout = options.Timeout > 0 ? options.Timeout : PortMappingConstants.DefaultTimeout; 
            cts.CancelAfter(timeout);                                                                                

            await portMapper.MapPorts(mappings.ToArray(), cts.Token, options.IsMappingDeletion).ConfigureAwait(false);
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
            List<Mapping> mappings;
            try
            { 
                mappings = PortMappingParser.ParseJson(tcp, udp, json);
            }
            catch(JsonReaderException)
            {
                logger.Error("The file did not contain valid json");
                return null;
            }
            
            if (mappings?.Count>0)
            {
                return mappings;
            }
            logger.Error("No information in the file matched the parameters supplied.");
            return null;
        }
    }
}
