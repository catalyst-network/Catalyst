using System;
using System.Collections.Generic;
using System.Linq;

namespace ADL.Util
{
    public static class ByteUtil
    {
        public static readonly byte[] EMPTY_BYTE_ARRAY = new byte[0];
        public static readonly byte[] ZERO_BYTE_ARRAY = {0};

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] CombineByteArr(params byte[][] arrays)
        {
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
        ///     Creates a copy of bytes and appends b to the end of it
        /// </summary>
        public static byte[] AppendByte(byte[] bytes, byte b)
        {
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
            if (end < 0)
            {
                end = org.Length + end;                
            }
            start = Math.Max(0, start);
            end = Math.Max(start, end);

            return org.Skip(start).Take(end - start).ToArray();
        }

        public static byte[] InitialiseEmptyByteArray(int length)
        {
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
        public static IEnumerable<byte> MergeToEnum(params byte[][] arrays)
        {
            foreach (var a in arrays)
            foreach (var b in a)
                yield return b;
        }

        /// <param name="arrays"> - arrays to merge </param>
        /// <returns> - merged array </returns>
        public static byte[] Merge(params byte[][] arrays)
        {
            return MergeToEnum(arrays).ToArray();
        }
        
        public static string ByteToString(byte[] array)
        {
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
            var length = Math.Min(a.Length, b.Length);
            var result = new byte[length];
            for (var i = 0; i < length; i++)
                result[i] = (byte) (a[i] ^ b[i]);
            return result;
        }
    }
}