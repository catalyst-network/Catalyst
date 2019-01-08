using System;
using System.Collections.Generic;
using System.Linq;

namespace ADL.Util
{
    public static class ByteUtil
    {
        public static byte[] ZeroByteArray { get; } = {0};
        private static readonly Random Rand = new Random();
        public static byte[] EmptyByteArray { get; } = new byte[0];

        /// <summary>
        /// returns a random 8 byte long ulong for use in message correlation
        /// </summary>
        /// <returns></returns>
        public static ulong GenerateCorrelationId()
        {
            var buf = new byte[8];
            Rand.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] CombineByteArr(params byte[][] arrays)
        {
            if (arrays == null) throw new ArgumentNullException(nameof(arrays));
            if (arrays.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(arrays));
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
        
        /// <summary>
        /// Creates a copy of bytes and appends b to the end of it
        /// </summary>
        public static byte[] AppendByte(byte[] bytes, byte b)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(bytes));
            var result = new byte[bytes.Length + 1];
            Array.Copy(bytes, result, bytes.Length);
            result[result.Length - 1] = b;
            return result;
        }

        /// <summary>
        /// Slice a section from byte array
        /// </summary>
        /// <param name="org"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static byte[] Slice(this byte[] org, int start, int end = int.MaxValue)
        {
            if (org == null) throw new ArgumentNullException(nameof(org));
            if (org.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(org));
            if (end < 0)
            {
                end = org.Length + end;                
            }
            start = Math.Max(0, start);
            end = Math.Max(start, end);

            return org.Skip(start).Take(end - start).ToArray();
        }

        /// <summary>
        /// @TODO replace all new byte with this method
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] InitialiseEmptyByteArray(int length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
            var returnArray = new byte[length];
            for (var i = 0; i < length; i++)
                returnArray[i] = 0x00;
            return returnArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        private static IEnumerable<byte> MergeToEnum(params byte[][] arrays)
        {
            if (arrays == null) throw new ArgumentNullException(nameof(arrays));
            if (arrays.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(arrays));
            foreach (var a in arrays)
            foreach (var b in a)
                yield return b;
        }

        /// <param name="arrays"> - arrays to merge </param>
        /// <returns> - merged array </returns>
        public static byte[] Merge(params byte[][] arrays)
        {
            if (arrays == null) throw new ArgumentNullException(nameof(arrays));
            if (arrays.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(arrays));
            return MergeToEnum(arrays).ToArray();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ByteToString(byte[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (array.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(array));
            return System.Text.Encoding.UTF8.GetString(array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] XOR(this byte[] a, byte[] b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a.Length == 0) throw new ArgumentException("Value a cannot be an empty collection.", nameof(a));
            if (b.Length == 0) throw new ArgumentException("Value b cannot be an empty collection.", nameof(b));

            var length = Math.Min(a.Length, b.Length);
            var result = new byte[length];
            for (var i = 0; i < length; i++)
                result[i] = (byte) (a[i] ^ b[i]);
            return result;
        }
    }
}
