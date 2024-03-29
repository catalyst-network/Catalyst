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

using System;
using System.Collections.Generic;
using System.Linq;
using Dawn;

namespace Catalyst.Core.Lib.Util
{
    public static class ByteUtil
    {
        private static readonly Random Rand = new Random();
        public static byte[] ZeroByteArray { get; } = {0};
        public static byte[] EmptyByteArray { get; } = new byte[0];

        /// <summary>
        ///     returns a random array of byte of the desired length
        /// </summary>
        public static byte[] GenerateRandomByteArray(int length)
        {
            var buf = new byte[length];
            Rand.NextBytes(buf);
            return buf;
        }

        /// <summary>
        ///     Creates a copy of bytes and appends b to the end of it
        /// </summary>
        public static byte[] AppendByte(byte[] array, byte b)
        {
            Guard.Argument(array, nameof(array)).NotNull().NotEmpty();

            var result = InitialiseEmptyByteArray(array.Length + 1);

            Array.Copy(array, result, array.Length);
            result[result.Length - 1] = b;
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] InitialiseEmptyByteArray(int length)
        {
            Guard.Argument(length, nameof(length)).Positive().NotZero();

            var returnArray = new byte[length];
            for (var i = 0; i < length; i++)
            {
                returnArray[i] = 0x00;
            }

            return returnArray;
        }

        /// <summary>
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        private static IEnumerable<byte> MergeToEnum(params byte[][] arrays)
        {
            Guard.Argument(arrays, nameof(arrays)).NotNull().NotEmpty();

            foreach (var a in arrays)
            {
                foreach (var b in a)
                {
                    yield return b;
                }
            }
        }

        /// <param name="arrays"> - arrays to merge </param>
        /// <returns> - merged array </returns>
        public static byte[] Merge(params byte[][] arrays)
        {
            return MergeToEnum(arrays).ToArray();
        }

        public class ByteListComparerBase : IComparer<IList<byte>>
        {
            protected ByteListComparerBase() { }

            public virtual int Compare(IList<byte> x, IList<byte> y)
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

                return 0;
            }
        }

        /// <remarks>
        /// Warning: this comparer assumes that the tail of the longest list is not relevant for comparison
        /// </remarks>
        public sealed class ByteListMinSizeComparer : ByteListComparerBase
        {
            public static IComparer<IList<byte>> Default { get; } = new ByteListMinSizeComparer();
        }

        public sealed class ByteListComparer : ByteListComparerBase
        {
            public override int Compare(IList<byte> x, IList<byte> y)
            {
                var baseCompare = base.Compare(x, y);
                return baseCompare != 0 ? baseCompare : Math.Sign(Nullable.Compare(x?.Count, y?.Count));
            }

            public static IComparer<IList<byte>> Default { get; } = new ByteListComparer();
        }
    }
}
