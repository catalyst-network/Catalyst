using System;
using CommandLine;

namespace Catalyst.Cli
{
    [Verb("get", HelpText = "Gets information from a catalyst node")]
    class GetInfoOptions
    {
        [Option('i', "info")]
        public bool i { get; set; }
        
        [Option('m', "mempool")]
        public bool m { get; set; }

        [Value(1, MetaName = "Node ID",
            HelpText = "Valid and connected node ID.",
            Required = true)]
        public string NodeId { get; set; }
    }
    
    [Verb("connect", HelpText = "Connects the CLI to a catalyst node")]
    class ConnectOptions
    {
        [Option('n', "info")]
        public bool n { get; set; }

        [Value(1, MetaName = "Node ID",
            HelpText = "Valid and connected node ID.",
            Required = true)]
        public string NodeId { get; set; }
    }
    
    /*public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }
    }*/
        
    
}