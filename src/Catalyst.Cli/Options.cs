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

using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Catalyst.Cli
{
    [Verb("get", HelpText = "Gets information from a catalyst node")]
    class GetInfoOptions
    {
        [Option('i', "info")]
        public bool Info { get; set; }

        [Option('m', "mempool")]
        public bool Mempool { get; set; }

        [Option('v', "version")]
        public bool Version { get; set; }

        [Value(1, MetaName = "Node ID",
            HelpText = "Valid and connected node ID.",
            Required = true)]
        public string NodeId { get; set; }
    }

    [Verb("connect", HelpText = "Connects the CLI to a catalyst node")]
    public sealed class ConnectOptions
    {
        [Option('n', "node", HelpText = "A valid node ID as listed in the nodes.json config file.")]
        public bool Node { get; set; }

        [Value(1, MetaName = "Node ID",
            HelpText = "Node name as listed in nodes.json config file.",
            Required = true)]
        public string NodeId { get; set; }

        [Usage(ApplicationAlias = "")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Connects the CLI to a node", new ConnectOptions
                {
                    NodeId = "Node ID"
                })
            };
    }

    [Verb("sign", HelpText = "Signs a message or a transaction")]
    public sealed class SignOptions
    {
        [Option('m', "message", HelpText = "Directs the CLI to sign the message to be provided as the value")]
        public string Message { get; set; }

        [Option('n', "node", HelpText = "A valid node ID as listed in the nodes.json config file.")]
        public string Node { get; set; }

        [Usage(ApplicationAlias = "")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Signs a message or a transaction provided.", new SignOptions
                {
                    Node = "Messsage"
                })
            };
    }

    [Verb("verify", HelpText = "verifies a message")]
    class VerifyOptions
    {
        [Option('m', "message", HelpText = "Directs the CLI to verify the message to be provided as the value")]
        public string Message { get; set; }

        [Option('k', "address", HelpText = "A valid public key.")]
        public string Address { get; set; }

        [Option('s', "signature", HelpText = "A valid signature.")]
        public string Signature { get; set; }

        [Option('n', "node", HelpText = "A valid node ID as listed in the nodes.json config file.")]
        public string Node { get; set; }

        [Usage(ApplicationAlias = "")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Signs a message or a transaction provided.", new SignOptions {Node = "Messsage"})
            };
    }

    /// <summary>
    /// Class contains the options for the peer list command
    /// </summary>
    [Verb("listpeers", HelpText = "displays peer list")]
    class PeerListOptions
    {
        /// <summary>
        /// Gets or sets the node.
        /// </summary>
        /// <value>
        /// The node.
        /// </value>
        [Option('n', "node", HelpText = "A valid node ID as listed in the nodes.json config file.")]
        public string Node { get; set; }

        /// <summary>
        /// Gets the examples.
        /// </summary>
        /// <value>
        /// The examples.
        /// </value>
        [Usage(ApplicationAlias = "")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Displays peer list for the specified node.", new PeerListOptions {Node = "node1"})
            };
    }

    /// <summary>
    /// Class contains the options for the peer list command
    /// </summary>
    [Verb("peerrep", HelpText = "displays the reputation of the peer")]
    class PeerReputationOptions
    {
        /// <summary>
        /// Gets or sets the node.
        /// </summary>
        /// <value>
        /// The node.
        /// </value>
        [Option('n', "node", HelpText = "A valid node ID as listed in the nodes.json config file.")]
        public string Node { get; set; }

        [Option('l', "ip", HelpText = "IP address of the peer whose reputation is of interest.")]
        public string IpAddress { get; set; }

        [Option('p', "publickey", HelpText = "Public key of the peer whose reputation is of interest.")]
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets the examples.
        /// </summary>
        /// <value>
        /// The examples.
        /// </value>
        [Usage(ApplicationAlias = "")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Displays peer the reputation for the specified node.", new PeerReputationOptions {Node = "node1"})
            };
    }

}
