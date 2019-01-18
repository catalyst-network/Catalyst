using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Catalyst.Helpers.RLP
{
    public static class ConverterForRlpEncodingExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static BigInteger ToBigIntegerFromRlpDecoded(this byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (BitConverter.IsLittleEndian)
            {
                var listEncoded = bytes.ToList();
                listEncoded.Insert(0, 0x00);
                bytes = listEncoded.ToArray().Reverse().ToArray();
                return new BigInteger(bytes);
            }

            return new BigInteger(bytes);
        }

        /// <summary>
        /// </summary>
        /// <param name="bigInteger"></param>
        /// <returns></returns>
        public static byte[] ToBytesForRlpEncoding(this BigInteger bigInteger)
        {
            return ToBytesFromNumber(bigInteger.ToByteArray());
        }

        /// <summary>
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static byte[] ToBytesForRlpEncoding(this int number)
        {
            return ToBytesFromNumber(BitConverter.GetBytes(number));
        }

        /// <summary>
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static byte[] ToBytesForRlpEncoding(this long number)
        {
            return ToBytesFromNumber(BitConverter.GetBytes(number));
        }

        /// <summary>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] ToBytesForRlpEncoding(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        public static byte[][] ToBytesForRlpEncoding(this string[] strings)
        {
            if (strings == null) throw new ArgumentNullException(nameof(strings));
            if (strings.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(strings));
            var output = new List<byte[]>();
            foreach (var str in strings)
                output.Add(str.ToBytesForRlpEncoding());
            return output.ToArray();
        }

        /// <summary>
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static int ToIntFromRlpDecoded(this byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(bytes));
            return (int) ToBigIntegerFromRlpDecoded(bytes);
        }

        /// <summary>
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static long ToLongFromRlpDecoded(this byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(bytes));
            return (long) ToBigIntegerFromRlpDecoded(bytes);
        }

        /// <summary>
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string ToStringFromRlpDecoded(this byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(bytes));
            return
                Encoding.UTF8.GetString(bytes, 0,
                    bytes.Length); //@TODO want to hook this into the  ByteToString util method but creates circular dep
        }

        /// <summary>
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static byte[] ToBytesFromNumber(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(bytes));
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse().ToArray();

            var trimmed = new List<byte>();
            var previousByteWasZero = true;

            for (var i = 0; i < bytes.Length; i++)
            {
                if (previousByteWasZero && bytes[i] == 0)
                    continue;

                previousByteWasZero = false;
                trimmed.Add(bytes[i]);
            }

            return trimmed.ToArray();
        }
    }
}