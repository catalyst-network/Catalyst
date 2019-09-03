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
    [Verb("sign", HelpText = "Signs a message or a transaction")]
    public sealed class SignOptions : OptionsBase, ISignOptions
    {
        /// <inheritdoc />
        [Option('m', "message", HelpText = "Directs the CLI to sign the message to be provided as the value")]
        public string Message { get; set; }
        
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
}
