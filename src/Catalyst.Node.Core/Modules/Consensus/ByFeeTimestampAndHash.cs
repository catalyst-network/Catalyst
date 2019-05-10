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
using System.Collections.Generic;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;

namespace Catalyst.Node.Core.Modules.Consensus
{
    /// <inheritdoc />
    public class TransactionComparerByFeeTimestampAndHash : ITransactionComparer
    {
        public int Compare(Transaction x, Transaction y)
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
            
            var signatureComparison = ByteListComparer.Default.Compare(xSignature, ySignature);
            if (signatureComparison != 0)
            {
                return signatureComparison;
            }

            var xComponent = x.SchnorrComponent.ToByteArray();
            var yComponent = y.SchnorrComponent.ToByteArray();
            return ByteListComparer.Default.Compare(xComponent, yComponent);
        }

        public static IComparer<TransactionSignature> Default { get; } = new SignatureComparer();
    }

    public class ByteListComparer : IComparer<IList<byte>>
    {
        public int Compare(IList<byte> x, IList<byte> y)
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

            for (var index = 0; index < Math.Min(x.Count, y.Count); index++)
            {
                var result = x[index].CompareTo(y[index]);
                if (result != 0)
                {
                    return Math.Sign(result);
                }
            }

            return x.Count.CompareTo(y.Count);
        }

        public static IComparer<IList<byte>> Default { get; } = new ByteListComparer();
    }
}
