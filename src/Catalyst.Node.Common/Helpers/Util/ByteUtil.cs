/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dawn;

namespace Catalyst.Node.Common.Helpers.Util
{
    public static class ByteUtil
    {
        private static readonly Random Rand = new Random();
        public static byte[] ZeroByteArray { get; } = {0};
        public static byte[] EmptyByteArray { get; } = new byte[0];

        /// <summary>
        ///     returns a random 8 byte long ulong for use in message correlation
        /// </summary>
        /// <returns></returns>
        public static ulong GenerateCorrelationId()
        {
            var buf = new byte[8];
            Rand.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        /// <summary>
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] CombineByteArrays(params byte[][] arrays)
        {
            Guard.Argument(arrays, nameof(arrays)).NotNull().NotEmpty();

            var rv = new byte[arrays.Sum(a => a.Length)];
            var offset = 0;
            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }

            return rv;
        }

        /// <summary>
        ///     Creates a copy of bytes and appends b to the end of it
        /// </summary>
        public static byte[] AppendByte(byte[] bytes, byte b)
        {
            Guard.Argument(bytes, nameof(bytes)).NotNull().NotEmpty();

            var result = new byte[bytes.Length + 1];
            Array.Copy(bytes, result, bytes.Length);
            result[result.Length - 1] = b;
            return result;
        }

        /// <summary>
        ///     Slice a section from byte array
        /// </summary>
        /// <param name="org"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static byte[] Slice(this byte[] bytes, int start, int end = int.MaxValue)
        {
            Guard.Argument(bytes, nameof(bytes)).NotNull().NotEmpty();
            Guard.Argument(start, nameof(start)).NotNegative();
            Guard.Argument(end, nameof(end)).InRange(start, int.MaxValue);

            return bytes.Skip(start).Take(end - start).ToArray();
        }

        /// <summary>
        ///     @TODO replace all new byte with this method
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] InitialiseEmptyByteArray(int length)
        {
            Guard.Argument(length, nameof(length)).Positive().NotZero();

            var returnArray = new byte[length];
            for (var i = 0; i < length; i++) returnArray[i] = 0x00;
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
            foreach (var b in a)
                yield return b;
        }

        /// <param name="arrays"> - arrays to merge </param>
        /// <returns> - merged array </returns>
        public static byte[] Merge(params byte[][] arrays) { return MergeToEnum(arrays).ToArray(); }

        /// <summary>
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ByteToString(byte[] bytes)
        {
            Guard.Argument(bytes, nameof(bytes)).NotNull().NotEmpty();
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] XOR(this byte[] a, byte[] b)
        {
            Guard.Argument(a, nameof(a)).NotNull().NotEmpty();
            Guard.Argument(b, nameof(b)).NotNull().NotEmpty();

            var length = Math.Min(a.Length, b.Length);
            var result = new byte[length];
            for (var i = 0; i < length; i++) result[i] = (byte) (a[i] ^ b[i]);
            return result;
        }
    }
}