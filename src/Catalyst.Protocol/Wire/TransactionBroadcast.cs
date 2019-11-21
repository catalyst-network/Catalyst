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

using System.Linq;
using System.Numerics;
using System.Reflection;
using Catalyst.Protocol.Cryptography;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Dirichlet.Numerics;
using Serilog;

namespace Catalyst.Protocol.Wire
{
    public partial class TransactionBroadcast
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        partial void OnConstruction()
        {
            IsContractDeployment = PublicEntries.Any(c => c.IsValidDeploymentEntry);
            IsContractCall = PublicEntries.Any(c => c.IsValidCallEntry);
            IsPublicTransaction = PublicEntries.Any() && PublicEntries.All(e => e.IsValid());
            IsConfidentialTransaction = ConfidentialEntries.Any()
             && ConfidentialEntries.All(e => e.IsValid());
        }

        /// <summary>
        /// OnConstruction does not generate valid transactions for tests / in general - need to discuss 
        /// </summary>
        public void AfterConstruction()
        {
            IsContractDeployment = PublicEntries.Any(c => c.IsValidDeploymentEntry);
            IsContractCall = PublicEntries.Any(c => c.IsValidCallEntry);
            IsPublicTransaction = PublicEntries.Any() && PublicEntries.All(e => e.IsValid());
            IsConfidentialTransaction = ConfidentialEntries.Any()
             && ConfidentialEntries.All(e => e.IsValid());
        }

        // why TransactionBroadcastTest is commented out? if brought back - this needs to have a test coverage there too
        public UInt256 AverageGasPrice
        {
            get
            {
                var averagePrice = BigInteger.Zero;
                ulong totalLimit = 0;
                var count = PublicEntries.Count;
                for (var i = 0; i < count; i++)
                {
                    var limit = PublicEntries[i].GasLimit;
                    totalLimit += limit;
                    averagePrice += limit * PublicEntries[i].GasPrice;
                }

                if (totalLimit == 0)
                {
                    return UInt256.Zero;
                }

                UInt256.Create(out var result, averagePrice / totalLimit);
                return result;
            }
        }

        // why TransactionBroadcastTest is commented out? if brought back - this needs to have a test coverage there too
        public ulong TotalGasLimit
        {
            get
            {
                ulong totalLimit = 0;
                var count = PublicEntries.Count;
                for (var i = 0; i < count; i++)
                {
                    totalLimit += PublicEntries[i].GasLimit;
                }

                return totalLimit;
            }
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

            Logger.Debug("{instance} can only be of a single type", nameof(TransactionBroadcast));
            return false;
        }

        public bool IsValid()
        {
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
    }
}
