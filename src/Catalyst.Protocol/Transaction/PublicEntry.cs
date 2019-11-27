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

using Catalyst.Protocol.Cryptography;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Dirichlet.Numerics;
using Serilog;
using System.Reflection;

namespace Catalyst.Protocol.Transaction
{
    public partial class PublicEntry
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        public bool IsValid()
        {
            // make it a constant - MIN_GAS_LIMIT
            if (GasLimit < 21000)
            {
                return false;
            }
            return true;

            var isTimestampValid = Timestamp != default(Timestamp) && Timestamp != new Timestamp();
            if (!isTimestampValid)
            {
                Logger.Debug("{timestamp} cannot be null or 0.");
                return false;
            }

            var hasValidSignature = Signature.IsValid(IsConfidentialTransaction
                ? SignatureType.TransactionConfidential
                : SignatureType.TransactionPublic);

            return hasValidSignature && HasValidEntries();
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

        public void BeforeConstruction()
        {
            IsContractDeployment = IsValidDeploymentEntry;
            IsContractCall = IsValidCallEntry;
            IsPublicTransaction = IsValid();
        }

        public void AfterConstruction()
        {
            IsContractDeployment = IsValidDeploymentEntry;
            IsContractCall = IsValidCallEntry;
            IsPublicTransaction = IsValid();
        }

        public bool IsContractDeployment { get; private set; }
        public bool IsContractCall { get; private set; }
        public bool IsPublicTransaction { get; private set; }
        public bool IsConfidentialTransaction { get; private set; }

        public bool HasValidEntries()
        {
            var hasSingleType = IsContractDeployment ^ IsContractCall ^ IsConfidentialTransaction;
            if (hasSingleType)
            {
                return true;
            }

            Logger.Debug("{instance} can only be of a single type", nameof(PublicEntry));
            return false;
        }
    }
}
