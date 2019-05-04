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

using Catalyst.Common.Interfaces.Cli.Options;
using CommandLine;

namespace Catalyst.Cli.Options
{
    [Verb("getmempool", HelpText = "Gets information from a catalyst node")]
    internal sealed class GetMempoolOptions : IGetMempoolOptions
    {
        [Option('m', "mempool")]
        public bool Mempool { get; set; }

        [Value(1, MetaName = "Node ID",
            HelpText = "Valid and connected node ID.",
            Required = true)]
        public string NodeId { get; set; }
    }
}
