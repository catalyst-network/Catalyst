using System;
using System.Text;

namespace Catalyst.Helpers.Hex.HexConverters.Extensions
{
    public static class HexStringUtf8ConverterExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string ToHexUtf8(this string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            return "0x" + Encoding.UTF8.GetBytes(value).ToHex();
        }

        /// <summary>
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string HexToUtf8String(this string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("Value cannot be null or empty.", nameof(hex));
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hex));
            var bytes = hex.HexToByteArray();
            return
                Encoding.UTF8.GetString(bytes, 0,
                    bytes.Length); //@TODO want to hook this into the  ByteToString util method but creates circular dep
        }
    }
}