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
    /// Get file onto DFS CLI options
    /// </summary>
    [Verb("getfile", HelpText = "gets file from dfs")]
    public sealed class GetFileOptions : OptionsBase, IGetFileOptions
    {
        /// <inheritdoc/>
        [Option('f', "file", HelpText = "The file hash", Required = true)]
        public string FileHash { get; set; }

        /// <inheritdoc/>
        [Option('o', "output", HelpText = "File output path", Required = true)]
        public string FileOutput { get; set; }

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
                new Example("Gets a file from DFS.", new GetFileOptions {Node = "node1", FileHash = "cfasvyu34t235237yh", FileOutput = "C:\\users\\user\\output.txt"})
            };
    }
}
