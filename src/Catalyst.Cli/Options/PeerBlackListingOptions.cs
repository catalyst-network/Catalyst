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
using Catalyst.Abstractions.Cli.Options;
using CommandLine;
using CommandLine.Text;

namespace Catalyst.Cli.Options
{
    /// <summary>
    /// Class contains the options for the peer list command
    /// </summary>
    [Verb("peerblacklist", HelpText = "displays the blacklist state of a peer")]
    public sealed class PeerBlackListingOptions : OptionsBase, IPeerBlackListingOptions
    {
        /// <inheritdoc />
        [Option('b', "blacklistflag", HelpText = "Blacklist flag for peer.")]
        public bool BlackListFlag { get; set; }

        /// <inheritdoc />
        [Option('i', "ip", HelpText = "IP address of the peer to blacklist.")]
        public string IpAddress { get; set; }

        /// <inheritdoc />
        [Option('p', "publickey", HelpText = "Public key of the peer to blacklist.")]
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
                new Example("Displays peer the blacklist state for the specified node.", new PeerBlackListingOptions {Node = "node1"})
            };
    }
}
