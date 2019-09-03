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
using Catalyst.Abstractions.Consensus;
using Catalyst.Core.Util;
using Catalyst.Protocol.Transaction;

namespace Catalyst.Core.Consensus
{
    /// <inheritdoc />
    public class TransactionComparerByFeeTimestampAndHash : ITransactionComparer
    {
        public int Compare(TransactionBroadcast x, TransactionBroadcast y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var feeComparison = x.TransactionFees.CompareTo(y.TransactionFees);
            if (feeComparison != 0)
            {
                return feeComparison;
            }

            var timeStampComparison = y.TimeStamp.CompareTo(x.TimeStamp);
            if (timeStampComparison != 0)
            {
                return timeStampComparison;
            }

            return SignatureComparer.Default.Compare(y.Signature, x.Signature);
        }

        public static ITransactionComparer Default { get; } = new TransactionComparerByFeeTimestampAndHash();
    }

    public class SignatureComparer : IComparer<TransactionSignature>
    {
        public int Compare(TransactionSignature x, TransactionSignature y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var xSignature = x.SchnorrSignature.ToByteArray();
            var ySignature = y.SchnorrSignature.ToByteArray();

            var signatureComparison = ByteUtil.ByteListMinSizeComparer.Default.Compare(xSignature, ySignature);
            if (signatureComparison != 0)
            {
                return signatureComparison;
            }

            var xComponent = x.SchnorrComponent.ToByteArray();
            var yComponent = y.SchnorrComponent.ToByteArray();
            return ByteUtil.ByteListMinSizeComparer.Default.Compare(xComponent, yComponent);
        }

        public static IComparer<TransactionSignature> Default { get; } = new SignatureComparer();
    }
}
