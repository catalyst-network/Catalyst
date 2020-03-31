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

using Google.Protobuf.WellKnownTypes;
using Serilog;
using System.Reflection;
using Nethermind.Dirichlet.Numerics;
using System;
using Google.Protobuf;
using MultiFormats;

namespace Catalyst.Protocol.Transaction
{
    public partial class PublicEntry
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        //Props like Amount and Gas price can contain null bytes appended to the end that can cause a different 
        //Id when converted back and forth. I assume this is comming from the web provider.
        private byte[] TrimEnd(byte[] array)
        {
            int lastIndex = Array.FindLastIndex(array, b => b != 0);

            Array.Resize(ref array, lastIndex + 1);

            return array;
        }

        //Give the same TransactionId everytime.
        public MultiHash GetId(string algorithmName)
        {
            var publicEntryClone = new PublicEntry(this);
            publicEntryClone.Amount = ByteString.CopyFrom(TrimEnd(publicEntryClone.Amount.ToByteArray()));
            publicEntryClone.GasPrice = ByteString.CopyFrom(TrimEnd(publicEntryClone.GasPrice.ToByteArray()));
            return MultiHash.ComputeHash(publicEntryClone.ToByteArray(), algorithmName);
        }

        public bool IsValid()
        {
            // make it a constant - MIN_GAS_LIMIT
            if (GasLimit < 21000)
            {
                return false;
            }

            var isTimestampValid = Timestamp != default && Timestamp != new Timestamp();
            if (!isTimestampValid)
            {
                Logger.Debug("{timestamp} cannot be null or 0.");
                return false;
            }

            return true;

            // TODO: reconsider signature

            //var hasValidSignature = Signature.IsValid(SignatureType.TransactionPublic);
            //return hasValidSignature;
        }

        /// <summary>bytes
        /// If this is an entry that is about to deploy a smart contract then <value>true</value>,
        /// otherwise <value>false</value>.
        /// </summary>
        public bool IsValidDeploymentEntry => IsValid() && ReceiverAddress.IsEmpty;

        /// <summary>
        /// If this is an entry that is about to call a smart contract then <value>true</value>,
        /// otherwise <value>false</value>.
        /// </summary>
        public bool IsValidCallEntry => IsValid() && !ReceiverAddress.IsEmpty;

        public bool IsContractDeployment => IsValidDeploymentEntry;
        public bool IsContractCall => IsValidCallEntry;
        public bool IsPublicTransaction => IsValid();
    }
}
