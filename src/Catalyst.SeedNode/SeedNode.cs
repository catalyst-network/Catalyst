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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces;
using Ipfs.CoreApi;
using Serilog;

namespace Catalyst.SeedNode
{
    public class SeedNode
        : ICatalystNode
    {
        private readonly ILogger _logger;
        private readonly ICoreApi _ipfs;

        public SeedNode(
           ICoreApi ipfs,
           ILogger logger)
        {
            _ipfs = ipfs;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var peer = await _ipfs.Generic.IdAsync();
            _logger.Information($"seed node {peer.Id}");
            foreach (var addr in peer.Addresses)
            {
                _logger.Information($"  listening on {addr}");
            }

            bool exit;
            do
            {
                _logger.Information("Type 'exit' to exit, anything else to continue");
                exit = string.Equals(Console.ReadLine(), "exit", StringComparison.OrdinalIgnoreCase);
            } while (!ct.IsCancellationRequested && !exit);

            _logger.Information("Stopping the seed node");
        }
    }
}
