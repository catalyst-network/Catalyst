using System;
using System.Linq;
using System.Numerics;

namespace Catalyst.Helpers.Hex.HexConverters.Extensions
{
    public static class HexBigIntegerConverterExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="littleEndian"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this BigInteger value, bool littleEndian)
        {
            byte[] bytes;
            bytes = BitConverter.IsLittleEndian != littleEndian
                ? value.ToByteArray().Reverse().ToArray()
                : value.ToByteArray().ToArray();
            return bytes;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="littleEndian"></param>
        /// <param name="compact"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string ToHex(this BigInteger value, bool littleEndian, bool compact = true)
        {
            if (value.Sign < 0)
                throw new Exception("Catalyst.Helpers.Hex Encoding of Negative BigInteger value is not supported");
            if (value == 0) return "0x0";

#if NETCOREAPP2_1
            var bytes = value.ToByteArray(true, !littleEndian);
#else
            var bytes = value.ToByteArray(littleEndian);
#endif

            if (compact)
                return "0x" + bytes.ToHexCompact();

            return "0x" + bytes.ToHex();
        }

        /// <summary>
        /// </summary>
        /// <param name="hex"></param>
        /// <param name="isHexLittleEndian"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static BigInteger HexToBigInteger(this string hex, bool isHexLittleEndian)
        {
            if (hex == null) throw new ArgumentNullException(nameof(hex));
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hex));
            if (hex == "0x0") return 0;

            var encoded = hex.HexToByteArray();

            if (BitConverter.IsLittleEndian != isHexLittleEndian)
            {
                var listEncoded = encoded.ToList();
                listEncoded.Insert(0, 0x00);
                encoded = listEncoded.ToArray().Reverse().ToArray();
            }

            return new BigInteger(encoded);
        }
    }
}