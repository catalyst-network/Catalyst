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

using System.Reflection;
using Catalyst.Protocol.Account;
using Nethermind.Dirichlet.Numerics;
using Serilog;

namespace Catalyst.Protocol.Transaction
{
    public partial class ContractEntry
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        public bool IsValid()
        {
            // data can be empty
            return GasLimit >= 21000; // make it a constant - MIN_GAS_LIMIT
        }

        // add to proto
        /// <summary>
        /// Gas limit for the entry expressed in gas units.
        /// </summary>
        public ulong GasLimit { get; set; }
        
        // add to proto
        /// <summary>
        /// Gas price to use as a multiplier of gas cost expressed in units
        /// to arrive at the total gas cost expressed in ETH.
        /// </summary>
        public UInt256 GasPrice { get; set; }
        
        /// <summary>
        /// If this is an entry that is about to deploy a smart contract then <value>true</value>,
        /// otherwise <value>false</value>.
        /// </summary>
        public bool IsValidDeploymentEntry => IsValid() && Base.ReceiverPublicKey.IsEmpty;
        
        /// <summary>
        /// If this is an entry that is about to call a smart contract then <value>true</value>,
        /// otherwise <value>false</value>.
        /// </summary>
        public bool IsValidCallEntry => IsValid() && !Base.ReceiverPublicKey.IsEmpty;
        
        public byte[] TargetContract { get; set; }
    }
}
