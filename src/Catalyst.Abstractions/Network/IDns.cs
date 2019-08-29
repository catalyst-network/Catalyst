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
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using DnsClient;

namespace Catalyst.Abstractions.Network
{
    public interface IDns
    {
        /// <summary>
        /// </summary>
        /// <param name="hostnames"></param>
        /// <returns></returns>
        Task<IList<IDnsQueryResponse>> GetTxtRecordsAsync(IList<string> hostnames);

        /// <summary>
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        Task<IDnsQueryResponse> GetTxtRecordsAsync(string hostname);

        /// <summary>
        ///     Returns a list of seed nodes from TXT records
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        IEnumerable<IPeerIdentifier> GetSeedNodesFromDns(IEnumerable<string> urls);
    }
}
