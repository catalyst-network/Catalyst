using System;
using CommandLine;

namespace Catalyst.Cli
{
    interface IOptions
    {
        /*[Option('g', "get", Required = false, HelpText = "Get command")]
        public bool Get { get; set; }*/
        
        [Option('i', "info", Required = false, HelpText = "Valid node id.")]
        bool Info { get; set; }
        
        [Value(0, MetaName = "Node ID",
            HelpText = "Valid and connected node ID.",
            Required = true)]
        string NodeId { get; set; }
    }
    
    [Verb("get", HelpText = "Gets node information")]
    class GetOptions : IOptions
    {
        public bool Info { get; set; }

        public string NodeId { get; set; }
    }
        
    
}