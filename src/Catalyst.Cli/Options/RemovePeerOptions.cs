#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using MultiFormats;

namespace Catalyst.Cli.Options
{
    /// <summary>
    /// Class contains the options for the peer list command
    /// </summary>
    [Verb("removepeer", HelpText = "removes a peer")]
    public sealed class RemovePeerOptions : OptionsBase, IRemovePeerOptions
    {
        [Option('a', "address", HelpText = "MultiAddress of the peer whose info is of interest.")]
        public string Address { get; set; }

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
                new Example("Removes a peer from the specified node.",
                    new MultiAddress("/ip4/192.168.0.181/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdwu2yu3QMzKa2BDkb5C5pcuhtrH1G9HHbztbbxA8tGmf4"))
            };
    }
}
