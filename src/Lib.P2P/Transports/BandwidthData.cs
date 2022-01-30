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

namespace Lib.P2P.Transports
{
    /// <summary>
    ///   The statistics for <see>
    ///       <cref>Catalyst.Ipfs.Core.CoreApi.IStatsApi.BandwidthAsync(System.Threading.CancellationToken)</cref>
    ///   </see>
    ///   .
    /// </summary>
    public class BandwidthData
    {
        /// <summary>
        ///   The number of bytes received.
        /// </summary>
        public ulong TotalIn;

        /// <summary>
        ///   The number of bytes sent.
        /// </summary>
        public ulong TotalOut;

        /// <summary>
        ///   TODO
        /// </summary>
        public double RateIn;

        /// <summary>
        ///   TODO
        /// </summary>
        public double RateOut;
    }
}
