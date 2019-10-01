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

using Catalyst.Abstractions.Consensus;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Util;

namespace Catalyst.Core.Modules.Consensus
{
    /// <inheritdoc />
    public class TransactionComparerByFeeTimestampAndHash : ITransactionComparer<TransactionBroadcastDao>
    {
        public int Compare(TransactionBroadcastDao x, TransactionBroadcastDao y)
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

            var feeComparison = x.SummedEntryFees().CompareTo(y.SummedEntryFees());
            if (feeComparison != 0)
            {
                return feeComparison;
            }

            var timeStampComparison = y.TimeStamp.CompareTo(x.TimeStamp);
            if (timeStampComparison != 0)
            {
                return timeStampComparison;
            }

            return ByteUtil.ByteListMinSizeComparer.Default.Compare(x.Signature.RawBytes.KeyToBytes(), y.Signature.RawBytes.KeyToBytes());
        }

        public static ITransactionComparer<TransactionBroadcastDao> Default { get; } = new TransactionComparerByFeeTimestampAndHash();
    }
}
