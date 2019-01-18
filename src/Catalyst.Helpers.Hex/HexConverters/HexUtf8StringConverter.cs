using System;
using Catalyst.Helpers.Hex.HexConverters.Extensions;

namespace Catalyst.Helpers.Hex.HexConverters
{
    public class HexUtf8StringConverter : IHexConvertor<string>
    {
        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string ConvertToHex(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Value cannot be null or empty.", nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            return value.ToHexUtf8();
        }

        /// <summary>
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string ConvertFromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) throw new ArgumentException("Value cannot be null or empty.", nameof(hex));
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(hex));
            return hex.HexToUtf8String();
        }
    }
}