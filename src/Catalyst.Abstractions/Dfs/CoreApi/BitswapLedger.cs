#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Lib.P2P;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Statistics on the <see cref="IBitSwapApi">bitswap</see> blocks exchanged with another <see cref="Peer"/>.
    /// </summary>
    /// <seealso cref="IBitSwapApi.GetBitSwapLedger"/>
    public class BitswapLedger
    {
        /// <summary>
        ///   The <see cref="Peer"/> that pertains to this ledger.
        /// </summary>
        /// <value>
        ///   The peer that is being monitored.
        /// </value>
        public Peer Peer { get; set; }

        /// <summary>
        ///   The number of blocks exchanged with the <see cref="Peer"/>.
        /// </summary>
        /// <value>
        ///   The number of blocks sent by the peer or sent by us to the peer.
        /// </value>
        public ulong BlocksExchanged { get; set; }

        /// <summary>
        ///   The number of bytes sent by the <see cref="Peer"/> to us.
        /// </summary>
        /// <value>
        ///   The number of bytes.
        /// </value>
        public ulong DataReceived { get; set; }

        /// <summary>
        ///   The number of bytes sent by us to the <see cref="Peer"/>
        /// </summary>
        /// <value>
        ///   The number of bytes.
        /// </value>
        public ulong DataSent { get; set; }

        /// <summary>
        ///   The calculated debt to the peer.
        /// </summary>
        /// <value>
        ///   <see cref="DataSent"/> divided by <see cref="DataReceived"/>.
        ///   A value less than 1 indicates that we are in debt to the 
        ///   <see cref="Peer"/>.
        /// </value>
        public float DebtRatio
        {
            get
            {
                return DataSent / (float) (DataReceived + 1); // +1 is to prevent division by zero
            }
        }

        /// <summary>
        ///   Determines if we owe the <see cref="Peer"/> some blocks.
        /// </summary>
        /// <value>
        ///   <b>true</b> if we owe data to the peer; otherwise, <b>false</b>.
        /// </value>
        public bool IsInDebt => DebtRatio < 1;
    }
}
